const path = require('path');
const { CleanWebpackPlugin } = require('clean-webpack-plugin');
const CopyWebpackPlugin = require('copy-webpack-plugin');

module.exports = {
  entry: {
    monitoring: './Front/src/monitoring.ts',
  },
  output: {
    filename: 'js/[name].bundle.js',
    path: path.resolve(__dirname, 'wwwroot'),
    publicPath: '/monitoring-static/'
  },
  resolve: {
    extensions: ['.ts', '.js']
  },
  module: {
    rules: [
      {
        test: /\.ts$/,
        use: 'ts-loader', // Fixed: removed style-loader and css-loader from here
        exclude: /node_modules/
      },
      {
        test: /\.css$/,  // Added: new rule for CSS files
        use: ['style-loader', 'css-loader']
      }
    ]
  },
  plugins: [
    new CleanWebpackPlugin({
      cleanOnceBeforeBuildPatterns: [
        '**/*',
        '!css/**', // Don't remove CSS files
        '!lib/**'  // Don't remove library files
      ]
    }),
    // Copy SignalR library files if needed
    new CopyWebpackPlugin({
      patterns: [
        {
          from: 'node_modules/@microsoft/signalr/dist/browser',
          to: 'lib/microsoft/signalr/dist/browser'
        },
        {
          from: 'node_modules/vis-timeline/styles', // Added: copy vis-timeline CSS
          to: 'lib/vis-timeline/styles'
        }
      ]
    })
  ],
  devtool: 'source-map',
  optimization: {
    splitChunks: {
      chunks: 'all',
      name: 'vendors'
    }
  },
  // Watch configuration remains unchanged
  watch: true,
  watchOptions: {
    aggregateTimeout: 300,
    poll: 1000,
    ignored: /node_modules/,
    followSymlinks: false
  }
};
