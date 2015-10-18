'use strict';

/* Controllers */

var automationControllers = angular.module('automationControllers', []);

automationControllers.controller('deviceListController', ['$scope', '$rootScope', '$q', '$http', 'deviceService',
    function ($scope, $rootScope, $q, $http, deviceService) {
        deviceService.getDevices().then(function (result) {
            $scope.devices = result.devices;
            $scope.deviceCategories = result.categories;
        });

        $scope.getStatus = function (device) {
            var status;

            if (device.Archetype == null)
                return;
            else if (device.Archetype == "lamp") {
                if (device.Dimmable == true)
                    status = String(device.Level * 100.0) + "%";
                else if (device.Value == true)
                    status = "On";
                else
                    status = "Off";
            }
            else if (device.Archetype == "curtain") {
                if (device.Position == 0)
                    status = "Down";
                else
                    status = "Up";
            }

            return status;
        };
    }]);

automationControllers.controller('deviceDetailsController', ['$scope', '$routeParams', 'deviceService', 'speechService',
    function ($scope, $routeParams, deviceService, speechService) {

        deviceService.getDevice(parseInt($routeParams.deviceId)).then(function (device) {
            $scope.device = device;
        });

        $scope.safeApply = function (fn) {
            var phase = this.$root.$$phase;
            if (phase == '$apply' || phase == '$digest') {
                if (fn) {
                    fn();
                }
            } else {
                this.$apply(fn);
            }
        };

        $scope.changeDevice = function (device) {
            console.log($scope.device);
            deviceService.changeDevice($scope.device).then(function (device) {
                console.log("New state");
                console.log(device)
                $scope.device = device;
            });
        }

        $scope.changeLampLevel = function (dimLevel) {
            //if (dimLevel != $scope.device.Level) {
                $scope.device.Value = (dimLevel > 0.0);
                $scope.device.Level = dimLevel;

                deviceService.changeDevice($scope.device).then(function (device) {
                    console.log("New state");
                    console.log(device)
                    $scope.device = device;
                });
            //}
        };

        $scope.changeLampValue = function (value) {
            //if (value != $scope.device.Value) {
                $scope.device.Value = value;
                if (value)
                    $scope.device.Level = 1.0;
                else
                    $scope.device.Level = 0.0;

                console.log($scope.device);
                deviceService.changeDevice($scope.device).then(function (device) {
                    console.log("New state");
                    console.log(device)
                    $scope.device = device;
                });
            //}
        };

        $scope.curtain = {
            down: function () {
                $scope.device.Action = 0;
                $scope.changeDevice($scope.device);
            },

            stop: function () {
                $scope.device.Action = 1;
                $scope.changeDevice($scope.device);
            },

            up: function () {
                $scope.device.Action = 2;
                $scope.changeDevice($scope.device);
            },
        };

        $scope.projector = {
            switchPower: function (value) {
                if (value == undefined)
                    // Switch instead
                    $scope.device.Power = !$scope.device.Power;
                else
                    $scope.device.Power = value;
                $scope.changeDevice($scope.device);
            },
        };

        $scope.receiver = {
            switchPower: function (value) {
                if (value == undefined)
                    // Toggle instead
                    $scope.device.Power = !$scope.device.Power;
                else
                    $scope.device.Power = value;
                $scope.changeDevice($scope.device);
            },

            mute: function (value) {
                if (value == undefined)
                    // Toggle instead
                    $scope.device.Mute = !$scope.device.Mute;
                else
                    $scope.device.Mute = value;
                $scope.changeDevice($scope.device);
            },

            incVolume: function () {
                $scope.device.Volume += 50;
                $scope.changeDevice($scope.device);
            },

            decVolume: function () {
                $scope.device.Volume -= 50;
                $scope.changeDevice($scope.device);
            },

            input: function (value) {
                $scope.device.Input = value;
                $scope.changeDevice($scope.device);
            },

            currentInput: function () {
                for (var i in $scope.device.Inputs) {
                    if ($scope.device.Inputs[i].Name === $scope.device.Input)
                        return $scope.device.Inputs[i];
                }

                return { DisplayName: "<Unknown>"}
            },
        };

        $scope.desktop = {
            switchPower: function (value) {
                if (value == undefined)
                    // Toggle instead
                    $scope.device.Power = !$scope.device.Power;
                else
                    $scope.device.Power = value;
                $scope.changeDevice($scope.device);
            },

            switchMonitor: function (value) {
                if (value == undefined)
                    // Toggle instead
                    $scope.device.MonitorPower = !$scope.device.MonitorPower;
                else
                    $scope.device.MonitorPower = value;
                $scope.changeDevice($scope.device);
            },
        };

        $scope.speech = {
            speak: function (message) {
                console.log("Speaking: " + message);
                speechService.speak(message);
            },
            recognize: function (text) {
                console.log("Trying to recognize text: " + text);
                speechService.recognize(text);
            }
        };
    }]);

automationControllers.controller('sceneController', ['$scope', '$http',
    function ($scope, $http) {

        $scope.scenes = [
            {
                DisplayName: "Cinema",
                Name: "cinema",
                Image: "success.png",
            },
            {
                DisplayName: "Night",
                Name: "night",
                Image: "success.png",
            },
            {
                DisplayName: "Foo",
                Name: "foo",
            },
        ];

        $scope.setScene = function (scene) {
            console.log("Setting scene: " + scene)

            var promise = $http({
                method: 'PUT',
                url: '/scene/' + scene.Name,
                data: scene,
                headers: {
                    'Content-Type': 'application/json',
                }
            })
        };
    }]);

automationControllers.controller('speechController', ['$scope', 'speechService',
    function ($scope, speechService) {

        $scope.speak = function (message) {
            controle.log("Speaking: " + message);
            speechService.speak(message);
        };
    }]);