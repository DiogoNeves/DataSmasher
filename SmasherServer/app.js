
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

//
// Routes
//

var smasherList = new Array(); // List of online smashers

// Index
app.get('/', routes.index);

// Smasher list

app.get('/smasherlist.json', function (req, res) {
	res.contentType("application/json");
	res.json(smasherList);
	res.end();
});

app.get('/smasherlist.json/filter/:self', function (req, res) {
	res.contentType("application/json");
	res.json(smasherList.filter(function (smasher) {
		return smasher != req.params.self;
	}));
	res.end();
});

app.get('/smasherlist', function (req, res) {
	res.render('list', { title: 'DataSmasher Server', server: 'http://127.0.0.1:3000', list: smasherList });
});

// Smasher information

app.get('/smasherinfo', routes.missing);

app.get('/smasherinfo/:smasheraddr', function (req, res) {
	if (smasherList.indexOf(req.params.smasheraddr) !== -1)
		res.render('smasher', { title: 'DataSmasher Server', smasher: req.params.smasheraddr });
	else
		res.render('smasher', { title: 'DataSmasher Server', smasher: null });
});

// Smasher manipulation

app.post('/addsmasher', function (req, res) {
	// Validate we have everything we need
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
		// Invalid parameters
		res.writeHead(500);
		res.end('Invalid parameters');
	}
});

app.listen(3000);
console.log("DataSmasher server listening on port %d in %s mode", app.address().port, app.settings.env);
