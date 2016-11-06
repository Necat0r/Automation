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

            if (device.archetype == null)
                return;
            else if (device.archetype == "lamp") {
                if (device.dimmable == true)
                    status = String(device.level * 100.0) + "%";
                else if (device.value == true)
                    status = "On";
                else
                    status = "Off";
            }
            else if (device.archetype == "curtain") {
                if (device.position == 0)
                    status = "Down";
                else
                    status = "Up";
            }
            else if (device.archetype == "receiver") {
                if (device.power == false)
                    status = "Off";
                else
                    status = String(device.volume / 10) + "dB";
            }

            return status;
        };
    }]);

automationControllers.controller('deviceDetailsController', ['$scope', '$routeParams', 'deviceService', 'speechService', 'eventService',
    function ($scope, $routeParams, deviceService, speechService, eventService) {

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
            //if (dimLevel != $scope.device.level) {
                $scope.device.value = (dimLevel > 0.0);
                $scope.device.level = dimLevel;

                deviceService.changeDevice($scope.device).then(function (device) {
                    console.log("New state");
                    console.log(device)
                    $scope.device = device;
                });
            //}
        };

        $scope.changeLampValue = function (value) {
            //if (value != $scope.device.value) {
                $scope.device.value = value;
                if (value)
                    $scope.device.level = 1.0;
                else
                    $scope.device.level = 0.0;

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
                $scope.device.action = 0;
                $scope.changeDevice($scope.device);
            },

            stop: function () {
                $scope.device.action = 1;
                $scope.changeDevice($scope.device);
            },

            up: function () {
                $scope.device.action = 2;
                $scope.changeDevice($scope.device);
            },
        };

        $scope.projector = {
            switchPower: function (value) {
                if (value == undefined)
                    // Switch instead
                    $scope.device.power = !$scope.device.power;
                else
                    $scope.device.power = value;
                $scope.changeDevice($scope.device);
            },
        };

        $scope.receiver = {
            switchPower: function (value) {
                if (value == undefined)
                    // Toggle instead
                    $scope.device.power = !$scope.device.power;
                else
                    $scope.device.power = value;
                $scope.changeDevice($scope.device);
            },

            mute: function (value) {
                if (value == undefined)
                    // Toggle instead
                    $scope.device.mute = !$scope.device.mute;
                else
                    $scope.device.mute = value;
                $scope.changeDevice($scope.device);
            },

            incVolume: function () {
                $scope.device.volume += 50;
                $scope.changeDevice($scope.device);
            },

            decVolume: function () {
                $scope.device.volume -= 50;
                $scope.changeDevice($scope.device);
            },

            input: function (value) {
                $scope.device.input = value;
                $scope.changeDevice($scope.device);
            },

            currentInput: function () {
                for (var i in $scope.device.inputs) {
                    if ($scope.device.inputs[i].name === $scope.device.input)
                        return $scope.device.inputs[i];
                }

                return { DisplayName: "<Unknown>"}
            },
        };

        $scope.desktop = {
            switchPower: function (value) {
                if (value == undefined)
                    // Toggle instead
                    $scope.device.power = !$scope.device.power;
                else
                    $scope.device.power = value;
                $scope.changeDevice($scope.device);
            },

            switchMonitor: function (value) {
                if (value == undefined)
                    // Toggle instead
                    $scope.device.monitorPower = !$scope.device.monitorPower;
                else
                    $scope.device.monitorPower = value;
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

        $scope.events = {
            sendEvent: function (name, data) {
                console.log("Sending event. " + name);
                eventService.sendEvent(name, data);
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
                url: '/scene/' + scene.name,
                data: scene,
                headers: {
                    'Content-Type': 'application/json',
                }
            })
        };
    }]);

automationControllers.controller('systemController', ['$scope', '$http',
    function ($scope, $http) {

        $http.get('/system/status')
        .then(function success(response) {
            console.log(response.data)
            $scope.status = response.data
        }, function fail(response) {
            console.log(response)
            $scope.status = 'Status retrieval failed';
        });

        $scope.update = function () {
            console.log("Updating")
            $http.put('/system/update', {});
        };

        $scope.restart = function () {
            console.log("Restarting")
            $http.put('system/restart', {});
        };

    }]);