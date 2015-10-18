'use strict';

var automationApp = angular.module('automationApp', [
    'ngRoute',
    'automationControllers',
    'automationServices'
]);

automationApp.config(['$routeProvider',
  function ($routeProvider) {
      $routeProvider.
        when('/devices', {
            templateUrl: 'partials/device-list.html',
            controller: 'deviceListController'
        }).
        when('/device/:deviceId', {
            templateUrl: 'partials/device-details.html',
            controller: 'deviceDetailsController'
        }).
        when('/scenes', {
            templateUrl: 'partials/scene-list.html',
            controller: 'sceneController'
        }).
        otherwise({
            redirectTo: '/devices'
        });
  }]);