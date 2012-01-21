
/**
 * Module dependencies.
 */

var express = require('express')
	, routes = require('./routes')

var app = module.exports = express.createServer();

// Configuration

app.configure(function(){
	app.set('views', __dirname + '/views');
	app.set('view engine', 'jade');
	app.use(express.bodyParser());
	app.use(express.methodOverride());
	app.use(app.router);
	app.use(express.static(__dirname + '/public'));
});

app.configure('development', function(){
	app.use(express.errorHandler({ dumpExceptions: true, showStack: true })); 
});

app.configure('production', function(){
	app.use(express.errorHandler()); 
});

// Routes

var smasherList = new Array();

app.get('/', routes.index);

app.get('/smasherlist.json', function (req, res) {
	res.contentType("application/json");
	res.json(smasherList);
	res.end();
});

app.get('/smasherlist', function (req, res) {
	res.send('test');
});

app.post('/addsmasher', function (req, res) {
	var address = req.body.Address;
	if (address != null && address !== undefined) {
		// Only add if we didn't yet
		if (smasherList.indexOf(address) === -1) {
			smasherList.push(req.body.Address);
			res.send('{response:"OK"}');
		}
		else {
			res.send('{response:"Already In"}');
		}
		res.end();
	}
	else {
		res.writeHead(500);
		res.end('Invalid parameters');
	}
});

app.listen(3000);
console.log("DataSmasher server listening on port %d in %s mode", app.address().port, app.settings.env);
