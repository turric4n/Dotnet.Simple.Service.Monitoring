const path = require('path');
const { CleanWebpackPlugin } = require('clean-webpack-plugin');
const CopyWebpackPlugin = require('copy-webpack-plugin');
const { WebpackManifestPlugin } = require('webpack-manifest-plugin');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');

module.exports = {
    entry: {
        monitoring: './Front/src/monitoring.ts',
    },
    output: {
        path: path.resolve(__dirname, 'wwwroot'),
        publicPath: '',  // Empty for relative paths in the manifest
        filename: 'js/[name].[contenthash].js', // Using contenthash instead of hash
        chunkFilename: 'js/[name].[chunkhash].js' // Chunks with chunk-specific hash
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
            },
            {
                test: /\.css$/,
                use: [
                    MiniCssExtractPlugin.loader,
                    'css-loader'
                ]
            }
        ]
    },
    plugins: [
        new CleanWebpackPlugin({
            cleanOnceBeforeBuildPatterns: [
                '**/*',
                '!css/**',
                '!lib/**'
            ]
        }),
        new CopyWebpackPlugin({
            patterns: [
                {
                    from: 'node_modules/@microsoft/signalr/dist/browser',
                    to: 'lib/microsoft/signalr/dist/browser'
                },
                {
                    from: 'node_modules/vis-timeline/styles',
                    to: 'lib/vis-timeline/styles'
                }
            ]
        }),
        new MiniCssExtractPlugin({
            filename: 'css/[name].[contenthash].css',
            chunkFilename: 'css/[id].[contenthash].css',
        }),
        new WebpackManifestPlugin({
            fileName: 'asset-manifest.json',
            publicPath: ''
        })
    ],
    devtool: 'source-map',
    optimization: {
        splitChunks: {
            chunks: 'all',
            name: 'vendors'
        }
    },
    watch: true,
    watchOptions: {
        aggregateTimeout: 300,
        poll: 1000,
        ignored: /node_modules/,
        followSymlinks: false
    }
};