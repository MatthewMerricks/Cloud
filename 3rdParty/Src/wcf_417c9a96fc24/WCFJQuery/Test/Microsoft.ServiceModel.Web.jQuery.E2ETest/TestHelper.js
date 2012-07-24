if (!this.TestHelper) {
    this.TestHelper = {};
}

jQuery.extend({
    getUrlVars: function () {
        var vars = [], val;
        var queryString = window.location.href.slice(window.location.href.indexOf('?') + 1).split('&');
        for (var i = 0; i < queryString.length; i++)
        {
            val = queryString[i].split('=');
            vars.push(val[0]);
            vars[val[0]] = val[1];
        }
        return vars;
    },
    getUrlVar: function (name) {
        return jQuery.getUrlVars()[name];
    }
});

TestHelper.getTestId = function() {
    var testId = jQuery.getUrlVar("testId");
    if (testId == null) {
        testId = '00000000-0000-0000-0000-000000000000';
    }
    return testId;
};

TestHelper.addTestHeader = function(req) {
    req.setRequestHeader("X-TestId", TestHelper.getTestId());
};

TestHelper.log = function(serverAddress, testId, info) {
    $.ajax({
        type: "POST",
        url: serverAddress + "/Log/" + testId,
        beforeSend: TestHelper.addTestHeader,
        data: JSON.stringify(info),
        contentType: "application/json"
    });
};

TestHelper.finishTest = function(serverAddress, testId, passed, failed) {
    $.ajax({
        type: "POST",
        url: serverAddress + "/TestResult/" + testId,
        beforeSend: TestHelper.addTestHeader,
        data: JSON.stringify({passed: passed, failed: failed}),
        contentType: "application/json"
    });
};

var testId = TestHelper.getTestId();

TestHelper.restartQUnit = function (req, textStatus) {
    TestHelper.log(TestHelper.logService, testId, "Restarting QUnit after ajax call with status " + textStatus);
    start();
};

TestHelper.genericAjaxError = function (req, textStatus, errorThrown) {
    ok(false, "$.ajax call failed; status=" + textStatus + ", errorThrown=" + errorThrown);
};

QUnit.log = function (result, message) {
    TestHelper.log(TestHelper.logService, testId, result + " :: " + message);
};

QUnit.done = function (failures, total) {
    var passed = total - failures;
    TestHelper.finishTest(TestHelper.logService, testId, passed, failures);
};

QUnit.moduleStart = function (name) {
    TestHelper.log(TestHelper.logService, testId, "Module start: " + name);
};

QUnit.moduleDone = function (name, failures, total) {
    TestHelper.log(TestHelper.logService,
                testId,
                "Module " + name + " finished. Assertions failed: " + failures + " of " + total + ".\n\n");
};

QUnit.testStart = function (name) {
    TestHelper.log(TestHelper.logService, testId, "Test start: " + name);
};

QUnit.testDone = function (name, failures, total) {
    TestHelper.log(TestHelper.logService, testId, "Test " + name + " finished. Assertions failed: " + failures + " of " + total + ".\n");
};
