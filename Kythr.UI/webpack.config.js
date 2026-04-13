const path = require('path');
const { CleanWebpackPlugin } = require('clean-webpack-plugin');
const CopyWebpackPlugin = require('copy-webpack-plugin');
const { WebpackManifestPlugin } = require('webpack-manifest-plugin');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');

module.exports = {
    entry: {
        monitoring: './Front/src/index.tsx',
    },
    output: {
        path: path.resolve(__dirname, 'wwwroot'),
        publicPath: '/monitoring-static/',
        filename: 'js/[name].[contenthash].js',
        chunkFilename: 'js/[name].[chunkhash].js'
    },
    resolve: {
        extensions: ['.tsx', '.ts', '.js'],
        alias: {
            '@': path.resolve(__dirname, 'Front/src')
        }
    },
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                use: 'ts-loader',
                exclude: /node_modules/
            },
            {
                test: /\.css$/,
                use: [
                    MiniCssExtractPlugin.loader,
                    'css-loader',
                    'postcss-loader'
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
    devtool: process.env.NODE_ENV === 'production' ? false : 'source-map',
    optimization: {
        splitChunks: {
            chunks: 'all',
            name: 'monitoring-vendors'
        }
    },
    watch: false,
    watchOptions: {
        aggregateTimeout: 300,
        poll: 1000,
        ignored: /node_modules/,
        followSymlinks: false
    }
};