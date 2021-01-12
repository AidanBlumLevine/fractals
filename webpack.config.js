var path = require('path');

module.exports = {
    mode: 'development',
    entry: './src/app.js',
    output: {
        path: path.resolve(__dirname, 'dist'),
        filename: 'pack.js'
    },
    module: {
        rules: [{
            test: /\.(vs|fs)$/i,
            use: 'raw-loader',
        }]
    }
};