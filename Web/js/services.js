'use strict';

/* Services */

var automationServices = angular.module('automationServices', []);

automationServices.factory('deviceService', ['$rootScope', '$timeout', '$http', '$q', function ($rootScope, $timeout, $http, $q) {
    
    var service = {

        //devices: [],
        //deviceCategories: [],

        refreshDevices: function() {
            var promise = $http.get('/device/list')
            .success(function (data) {
                var lamps = [];
                var curtains = [];
                var other = []

                for (var i in data) {
                    data[i]['index'] = i;

                    // Only include decies with proper display names
                    if (data[i].displayName == null)
                        continue;

                    if (data[i].archetype == null)
                        other.push(i);
                    else if (data[i].archetype == "lamp")
                        lamps.push(i);
                    else if (data[i].archetype == "curtain")
                        curtains.push(i);
                    else
                        other.push(i);
                }

                service['devices'] = data;
                service['deviceCategories'] = [
                    { name: "LAMPS", devices: lamps },
                    { name: "CURTAINS", devices: curtains },
                    { name: "OTHER", devices: other }];
            });

            return promise;
        },

        getDevice: function (index) {
            var deferred = $q.defer();
            var promise = deferred.promise;

            if ('devices' in service) {
                deferred.resolve(service.devices[index]);
            }
            else {
                service.refreshDevices().then(function () {
                    deferred.resolve(service.devices[index]);
                });
            };

            return promise;
        },

        getDevices: function () {
            var deferred = $q.defer();
            var promise = deferred.promise;

            if ('devices' in service) {
                $timeout(function () {
                    deferred.resolve({ devices: service.devices, categories: service.deviceCategories });
                });
            }
            else {
                service.refreshDevices().then(function () {
                    $timeout(function () {
                        deferred.resolve({ devices: service.devices, categories: service.deviceCategories });
                    });
                });
            };

            return promise;
        },

        changeDevice: function (device) {
            var index = device.index;

            var promise = $http({
                method: 'PUT',
                url: '/device/status/' + device.name,
                data: device,
                headers: {
                    'Content-Type': 'application/json',
                }})
            .success(function (deviceResponse) {
                // Add back our index
                deviceResponse['index'] = index;
                service.devices[index] = deviceResponse;
            });

            return promise.then(function (response) {
                return response.data;
            });
        }
    }

    return service;
}]);

automationServices.factory('speechService', ['$rootScope', '$timeout', '$http', '$q', function ($rootScope, $timeout, $http, $q) {
    var speechService = {};

    speechService.speak = function (message) {
        $http({
            method: 'PUT',
            url: '/speech/speak?text=' + message,
        });
    }

    speechService.recognize = function (message) {
        var promise = $http({
            method: 'PUT',
            url: '/speech/recognize?text=' + message,
        });
    }

    return speechService;

}]);

automationServices.factory('eventService', ['$rootScope', '$timeout', '$http', '$q', function ($rootScope, $timeout, $http, $q) {
    var eventService = {};

    eventService.sendEvent = function (name, data) {
        $http({
            method: 'PUT',
            url: '/events',
            data: { Name: name, Data: data },
            headers: {
                'Content-Type': 'application/json',
            }
        });
    }

    return eventService;
}]);