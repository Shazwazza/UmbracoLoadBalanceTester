var lbTester = angular.module('lbTester', []);

lbTester.controller('LbTesterCtrl', function ($scope, $http, $timeout) {

    var serverUrls = [
        "http://localhost:7300/",
        "http://lbtest1.dev/",
        "http://lbtest2.dev/"
    ];
    $scope.servers = [];
    for (var i = 0; i < 3; i++) {
        $scope.servers.push({
            pageStatus: "idle",
            internalSearchStatus: "idle",
            externalSearchStatus: "idle",
            totalPublished: {},
            url: serverUrls[i]
        });
    }
    $scope.currentPageId = null;
    $scope.cacheError = false;
    $scope.dbError = false;

    $scope.checkStatus = function (contentId) {
        for (var s in $scope.servers) {

            $scope.servers[s].pageStatus = "checking";
            $scope.servers[s].internalSearchStatus = "checking";
            $scope.servers[s].externalSearchStatus = "checking";

            checkServerStatus($scope.servers[s], contentId);
            checkInternalSearch($scope.servers[s], contentId);
            checkExternalSearch($scope.servers[s], contentId);
        }
    }

    function getPublishedCounts() {
        angular.forEach($scope.servers, function (s) {
            $http.get(s.url + "umbraco/api/LBTestingKit/GetPublishedCount").
                success(function (data, status, headers, config) {
                    s.totalPublished = data;

                    checkPublishedCounts();
                }).
                error(function (data, status, headers, config) {
                    alert("ERROR!");
                });
        });

        
    }
    
    

    function checkPublishedCounts() {
        var lastDb, lastCached;
        $scope.cacheError = false;
        $scope.dbError = false;
        angular.forEach($scope.servers, function (s) {
            if (!lastDb) {
                lastDb = s.totalPublished.DatabasePublished;
            }
            else if (lastDb != s.totalPublished.DatabasePublished) {
                $scope.dbError = true;
            }
            if (!lastCached) {
                lastCached = s.totalPublished.CachePublished;
            }
            else if (lastCached != s.totalPublished.CachePublished) {
                $scope.cacheError = true;
            }
        });
    }

    //initialize the data
    getPublishedCounts();

    function performCheck(server, contentId, url, statusName, retry) {
        $http.get(url).
              success(function (data, status, headers, config) {
                  server[statusName] = "ok";
              }).
              error(function (data, status, headers, config) {

                  if (retry == undefined) {
                      retry = 0;
                  }

                  server[statusName] = contentId + " not found (retry: " + retry + ")";

                  //retry!
                  $timeout(function () {

                      if (retry < 10) {
                          performCheck(server, contentId, url, statusName, ++retry);
                      }
                      else {
                          server[statusName] = "error";
                      }

                  }, 1000);
              });

        getPublishedCounts();
    }

    function checkExternalSearch(server, contentId) {
        performCheck(server, contentId, server.url + "umbraco/api/LBTestingKit/SearchExternalId/" + contentId, "externalSearchStatus")
    }

    function checkInternalSearch(server, contentId) {
        performCheck(server, contentId, server.url + "umbraco/api/LBTestingKit/SearchInternalId/" + contentId, "internalSearchStatus")
    }

    function checkServerStatus(server, contentId) {
        performCheck(server, contentId, server.url + contentId, "pageStatus")
    }

    $scope.publishContent = function (serverIndex) {

        for (var s in $scope.servers) {
            $scope.servers[s].pageStatus = "publishing";
            $scope.servers[s].internalSearchStatus = "idle";
            $scope.servers[s].externalSearchStatus = "idle";
        }

        $http.post("/api/status/" + serverIndex).
          success(function (data, status, headers, config) {
              for (var s in $scope.servers) {
                  $scope.servers[s].pageStatus = "checking: " + data;
              }
              $scope.currentPageId = data;
              $scope.checkStatus(data);
          }).
          error(function (data, status, headers, config) {
              alert("ERROR!");
          });
    }

});