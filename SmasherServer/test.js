var http = require('http');
var urlParser = require('url');

var SmasherManager = function () {

  this.addSmasher = function (smasherIp) {
    console.log("Adding " + smasherIp);
  };

  this.removeSmasher = function (smasherIp) {
    console.log("Removing " + smasherIp);
  };
};

var manager = new SmasherManager();


// Entry point for the server
http.createServer(function (req, res) {

  var urlInfo = urlParser.parse(req.url);

  // our rule, POST gets JSON back
  // and GET gets plain text back.

  // Ok, we've got a request...
  if (req.method === 'POST') {
    
    res.writeHead(200, {'Content-Type': 'application/json'});

    // are we adding a new Smasher?
    if (urlInfo.pathname === "/addsmasher") {
      manager.addSmasher();
      res.write("{msg:'added smasher'}");
    }
    // are we removing a Smasher?
    else if (urlInfo.pathname === "/removesmasher") {
      manager.removeSmasher();
      res.write("{msg:'removed smasher'}");
    }
  }
  else if (req.method === 'GET') {

    res.writeHead(200, {'Content-Type': 'text/plain'});

    // are we getting the list of smashers?
    if (urlInfo.pathname === "/smasherlist") {
      res.write("Get list for " + urlInfo.query);
    }

    res.end('\nHello World\n');
  }

  // Ignore favicon for now
  if (urlInfo.pathname === "/favicon.ico")
    return 0;

  console.log(urlInfo);
}).listen(1337, "127.0.0.1");


console.log('Server running at http://127.0.0.1:1337/');
