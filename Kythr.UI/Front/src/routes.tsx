import { lazy } from 'react';
import { createBrowserRouter, Navigate } from 'react-router-dom';
import { AppLayout } from '@/components/layout';

const Dashboard = lazy(() => import('@/pages/Dashboard'));
const ServiceDetail = lazy(() => import('@/pages/ServiceDetail'));
const Configuration = lazy(() => import('@/pages/Configuration'));
const Alerts = lazy(() => import('@/pages/Alerts'));
const Settings = lazy(() => import('@/pages/Settings'));
const NotFound = lazy(() => import('@/pages/NotFound'));

export const router = createBrowserRouter(
  [
    {
      path: '/',
      element: <AppLayout />,
      children: [
        { index: true, element: <Dashboard /> },
        { path: 'service/:name', element: <ServiceDetail /> },
        { path: 'configuration', element: <Configuration /> },
        { path: 'alerts', element: <Alerts /> },
        { path: 'settings', element: <Settings /> },
        { path: '404', element: <NotFound /> },
        { path: '*', element: <Navigate to="/404" replace /> },
      ],
    },
  ],
  { basename: '/monitoring' }
);
