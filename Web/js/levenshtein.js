﻿'use strict';

var levenshtein = function (str1, str2) {
    var cost;

    // get values
    var a = str1;
    var m = a.length;

    var b = str2;
    var n = b.length;

    // make sure a.length >= b.length to use O(min(n,m)) space, whatever that is
    if (m < n) {
        var c = a; a = b; b = c;
        var o = m; m = n; n = o;
    }

    var r = new Array();
    r[0] = new Array();
    for (var c = 0; c < n + 1; c++) {
        r[0][c] = c;
    }

    for (var i = 1; i < m + 1; i++) {
        r[i] = new Array();
        r[i][0] = i;
        for (var j = 1; j < n + 1; j++) {
            cost = (a.charAt(i - 1) == b.charAt(j - 1)) ? 0 : 1;
            r[i][j] = minimator(r[i - 1][j] + 1, r[i][j - 1] + 1, r[i - 1][j - 1] + cost);
        }
    }

    var maxLen = Math.max(m, n);

    // Accuracy
    return (maxLen - r[m][n]) / maxLen;
}

// return the smallest of the three values passed in
var minimator = function (x, y, z) {
    if (x < y && x < z) return x;
    if (y < x && y < z) return y;
    return z;
}