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
        use: 'ts-loader',
        exclude: /node_modules/
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
  // Add watch configuration
  watch: true,
  watchOptions: {
    aggregateTimeout: 300, // Delay before rebuilding
    poll: 1000, // Check for changes every second (useful in VMs or containers)
    ignored: /node_modules/,
    followSymlinks: false
  }
};
