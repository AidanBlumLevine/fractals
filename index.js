var express = require('express'),
    app = express(),
    http = require('http')
app.use(express.static(__dirname + '/'));

app.get('/', function (req, res) {
    res.sendFile(__dirname + '/index.html');
});
app.listen(3000);