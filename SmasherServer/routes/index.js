
/*
 * GET home page.
 */

exports.index = function(req, res) {
  res.render('index', { title: 'DataSmasher Server' });
};

exports.missing = function(req, res) {
	res.render('index', { title: 'DataSmasher Server - invalid parameters' });
};