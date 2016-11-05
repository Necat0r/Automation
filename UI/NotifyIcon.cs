using System;
using System.Threading;
using System.Windows.Forms;

namespace UI
{
    public class NotifyIcon : IDisposable
    {
        private System.Windows.Forms.NotifyIcon notifyIcon;

        public NotifyIcon(System.Drawing.Icon icon)
        {
            var menu = new ContextMenu();

            // Setup menu items
            int index = 0;
            var showItem = new MenuItem();
            showItem.Index = index++;
            showItem.DefaultItem = true;
            showItem.Text = "&Show";
            showItem.Click += new EventHandler(this.OnClickShow);
            menu.MenuItems.Add(showItem);

            var sep = new MenuItem();
            sep.Index = index++;
            sep.Text = "-";
            menu.MenuItems.Add(sep);

            var exitItem = new MenuItem();
            exitItem.Index = index++;
            exitItem.Text = "E&xit";
            exitItem.Click += new EventHandler(this.OnClickExit);
            menu.MenuItems.Add(exitItem);

            // Create the NotifyIcon. 
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(new System.ComponentModel.Container());

            // The Icon property sets the icon that will appear 
            // in the systray for this application.
            notifyIcon.Icon = icon;

            // The ContextMenu property sets the menu that will 
            // appear when the systray icon is right clicked.
            notifyIcon.ContextMenu = menu;

            // The Text property sets the text that will be displayed, 
            // in a tooltip, when the mouse hovers over the systray icon.
            notifyIcon.Text = "Automation server";
            notifyIcon.Visible = true;

            // Handle the DoubleClick event to activate the form.
            notifyIcon.DoubleClick += new System.EventHandler(this.OnClickShow);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (notifyIcon != null)
                    notifyIcon.Dispose();
                notifyIcon = null;
            }
        }

        public void SetStatus(string status)
        {
            //notifyIcon.Text = status;
        }

        private void OnClickShow(object Sender, EventArgs e)
        {
        }

        private void OnClickExit(object Sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            Application.ExitThread();
        }
    }
}
