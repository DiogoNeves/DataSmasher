
/*
 * GET home page.
 */

exports.index = function(req, res) {
  res.render('index', { title: 'DataSmasher Server', server: '127.0.0.1:3000' });
};

exports.missing = function(req, res) {
	res.render('index', { title: 'DataSmasher Server - invalid parameters', server: '127.0.0.1:3000' });
};