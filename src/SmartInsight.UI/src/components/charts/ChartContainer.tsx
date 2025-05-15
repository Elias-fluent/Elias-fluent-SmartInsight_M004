import React, { ReactNode } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Skeleton } from '../ui/skeleton';

export interface ChartContainerProps {
  title: string;
  description?: string;
  isLoading?: boolean;
  error?: string | null;
  height?: string | number;
  width?: string | number;
  className?: string;
  children: ReactNode;
}

/**
 * A container component for visualizations that provides consistent styling,
 * loading states, error handling, and responsiveness.
 */
export const ChartContainer: React.FC<ChartContainerProps> = ({
  title,
  description,
  isLoading = false,
  error = null,
  height = 300,
  width = '100%',
  className = '',
  children,
}) => {
  const containerStyles = {
    height: typeof height === 'number' ? `${height}px` : height,
    width: typeof width === 'number' ? `${width}px` : width,
  };

  const renderContent = () => {
    if (isLoading) {
      return (
        <div className="flex flex-col h-full items-center justify-center p-6">
          <Skeleton className="h-[200px] w-full" />
        </div>
      );
    }

    if (error) {
      return (
        <div className="flex flex-col h-full items-center justify-center p-6 text-destructive">
          <div className="text-center">
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="mx-auto mb-2">
              <circle cx="12" cy="12" r="10"></circle>
              <line x1="12" y1="8" x2="12" y2="12"></line>
              <line x1="12" y1="16" x2="12.01" y2="16"></line>
            </svg>
            <h3 className="mt-2 text-sm font-medium">Error Loading Chart</h3>
            <p className="mt-1 text-sm opacity-70">{error}</p>
          </div>
        </div>
      );
    }

    return (
      <div className="h-full w-full">
        {children}
      </div>
    );
  };

  return (
    <Card className={`overflow-hidden ${className}`} style={containerStyles}>
      <CardHeader className="p-4 pb-2">
        <CardTitle className="text-lg font-medium">{title}</CardTitle>
        {description && <CardDescription>{description}</CardDescription>}
      </CardHeader>
      <CardContent className="p-0">
        {renderContent()}
      </CardContent>
    </Card>
  );
};

export default ChartContainer; 