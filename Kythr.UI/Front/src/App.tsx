import { Suspense } from 'react';
import { RouterProvider } from 'react-router-dom';
import { TooltipProvider } from '@/components/ui/tooltip';
import { QueryProvider } from '@/providers/QueryProvider';
import { ErrorBoundary } from '@/components/shared/ErrorBoundary';
import { router } from '@/routes';
import { Skeleton } from '@/components/ui/skeleton';

function LoadingFallback() {
  return (
    <div className="flex min-h-screen items-center justify-center">
      <div className="space-y-4 w-64">
        <Skeleton className="h-8 w-full" />
        <Skeleton className="h-4 w-3/4" />
        <Skeleton className="h-4 w-1/2" />
      </div>
    </div>
  );
}

export default function App() {
  return (
    <ErrorBoundary>
      <QueryProvider>
        <TooltipProvider delayDuration={300}>
          <Suspense fallback={<LoadingFallback />}>
            <RouterProvider router={router} />
          </Suspense>
        </TooltipProvider>
      </QueryProvider>
    </ErrorBoundary>
  );
}
