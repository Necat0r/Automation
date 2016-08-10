using Logging;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Speech
{
    class AudioInput : IDisposable
    {
        // Shamelessly copied from http://stackoverflow.com/questions/1682902/streaming-input-to-system-speech-recognition-speechrecognitionengine
        // TODO - Reimplement before any public release
        class SpeechStreamer : Stream
        {
            private AutoResetEvent _writeEvent;
            private List<byte> _buffer;
            private int _buffersize;
            private int _readposition;
            private int _writeposition;
            private bool _reset;

            public SpeechStreamer(int bufferSize)
            {
                _writeEvent = new AutoResetEvent(false);
                _buffersize = bufferSize;
                _buffer = new List<byte>(_buffersize);
                for (int i = 0; i < _buffersize; i++)
                    _buffer.Add(new byte());
                _readposition = 0;
                _writeposition = 0;
            }

            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanSeek
            {
                get { return false; }
            }

            public override bool CanWrite
            {
                get { return true; }
            }

            public override long Length
            {
                get { return -1L; }
            }

            public override long Position
            {
                get { return 0L; }
                set { }
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return 0L;
            }

            public override void SetLength(long value)
            {

            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int i = 0;
                while (i < count && _writeEvent != null)
                {
                    if (!_reset && _readposition >= _writeposition)
                    {
                        _writeEvent.WaitOne(100, true);
                        continue;
                    }
                    buffer[i] = _buffer[_readposition + offset];
                    _readposition++;
                    if (_readposition == _buffersize)
                    {
                        _readposition = 0;
                        _reset = false;
                    }
                    i++;
                }

                return count;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                for (int i = offset; i < offset + count; i++)
                {
                    _buffer[_writeposition] = buffer[i];
                    _writeposition++;
                    if (_writeposition == _buffersize)
                    {
                        _writeposition = 0;
                        _reset = true;
                    }
                }
                _writeEvent.Set();

            }

            public override void Close()
            {
                _writeEvent.Close();
                _writeEvent = null;
                base.Close();
            }

            public override void Flush()
            {

            }
        }

        public Stream mStream = new SpeechStreamer(100000);
        private WaveInEvent mWaveIn;

        private object mLock = new Object();
        private volatile bool mRecording = false;

        public AudioInput()
        {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            // Kick off our voice task.
            var task = Task.Run(async () =>
            {
                var enumerator = new MMDeviceEnumerator();
                string deviceId = null;

                while (!token.IsCancellationRequested)
                {
                    string newDeviceId = null;
                    try
                    {
                        try
                        {
                            MMDevice endpoint = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
                            newDeviceId = endpoint.ID;
                        }
                        catch
                        {}

                        // Default endpoint changed.
                        if (mWaveIn != null && (newDeviceId != deviceId))
                        {
                            deviceId = newDeviceId;

                            // We've switched current default device
                            Log.Info("Input device changed.");

                            mRecording = false;
                            DisposeWaveIn();
                        }

                        deviceId = newDeviceId;

                        // On-demand create mWaveIn
                        if (mWaveIn == null && deviceId != null)
                        {
                            CreateWaveIn();
                        }

                        // Try start the recording
                        if (mWaveIn != null && !mRecording)
                        {
                            lock (mLock)
                            {
                                try
                                {
                                    mWaveIn.StartRecording();
                                    mRecording = true;
                                    Log.Info("Recording started");
                                }
                                catch (NAudio.MmException exception)
                                {
                                    Log.Debug("NAudio exception when starting recording. Exception: {0}", exception.Message);

                                    // Purge instance to force a recreate next turn.
                                    DisposeWaveIn();
                                }
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        // Eat it all to prevent the voice task from dying due to random exceptions we haven't noticed yet.
                        Log.Error("Unhandled exception in voice task. Exception: {0}", exception.Message);
                    }

                    // It's enough if we poll once a second.
                    await Task.Delay(1000);
                }
                Log.Info("Finishing voice task");
            });
        }

        public void Dispose()
        {
            mStream.Dispose();
            mWaveIn.Dispose();
        }

        private void CreateWaveIn()
        {
            if (mWaveIn != null)
            {
                Debug.Assert(mWaveIn == null, "mWaveIn already created");
                return;
            }

            mWaveIn = new WaveInEvent();
            mWaveIn.WaveFormat = new WaveFormat(44100, 1);
            mWaveIn.DataAvailable += new EventHandler<WaveInEventArgs>(OnAudioDataAvailable);
            mWaveIn.RecordingStopped += new EventHandler<StoppedEventArgs>(OnAudioRecordingStopped);
        }

        private void DisposeWaveIn()
        {
            try
            {
                mWaveIn.DataAvailable -= new EventHandler<WaveInEventArgs>(OnAudioDataAvailable);
                mWaveIn.RecordingStopped -= new EventHandler<StoppedEventArgs>(OnAudioRecordingStopped);
                mWaveIn.Dispose();
            }
            catch (NAudio.MmException exception)
            {
                Log.Debug("NAudio exception disposing WaveIn. Exception: {0}", exception.Message);
            }
            mWaveIn = null;
        }

        private void OnAudioDataAvailable(object sender, WaveInEventArgs e)
        {
            mStream.Write(e.Buffer, 0, e.Buffer.Length);
        }

        private void OnAudioRecordingStopped(object sender, StoppedEventArgs e)
        {
            lock (mLock)
            {
                Log.Info("Recording stopped");
                mRecording = false;
            }
        }

    }
}
