import React, { useMemo } from 'react';
import {
  ScatterChart as RechartsScatterChart,
  Scatter,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  ZAxis
} from 'recharts';
import { ChartContainer, type ChartContainerProps } from './ChartContainer';

export interface ScatterDataPoint {
  x: number;
  y: number;
  z?: number; // Optional third dimension for bubble size
  name?: string; // For tooltip labeling
  [key: string]: any; // Additional properties
}

export interface ScatterSeries {
  name: string;
  data: ScatterDataPoint[];
  color?: string;
  shape?: 'circle' | 'cross' | 'diamond' | 'square' | 'star' | 'triangle' | 'wye';
  opacity?: number;
}

export interface ScatterPlotProps extends Omit<ChartContainerProps, 'children'> {
  series: ScatterSeries[];
  xAxisLabel?: string;
  yAxisLabel?: string;
  zAxisRange?: [number, number]; // Range for bubble sizes
  xDomain?: [number, number]; // Optional custom domain for x-axis
  yDomain?: [number, number]; // Optional custom domain for y-axis
  showGrid?: boolean;
  bubble?: boolean; // If true, uses z value for point size
}

/**
 * Scatter Plot component that wraps Recharts ScatterChart with our standard styling and features.
 */
export const ScatterPlot: React.FC<ScatterPlotProps> = ({
  series,
  xAxisLabel,
  yAxisLabel,
  zAxisRange = [10, 100], // Default bubble size range
  xDomain,
  yDomain,
  showGrid = true,
  bubble = false,
  ...containerProps
}) => {
  // Default colors for scatter series if not specified
  const defaultColors = [
    '#0088FE', '#00C49F', '#FFBB28', '#FF8042', 
    '#8884D8', '#82CA9D', '#FFC658', '#8DD1E1'
  ];

  // Apply default colors and shapes to series that don't have them specified
  const seriesWithColors = useMemo(() => {
    const shapes = ['circle', 'cross', 'diamond', 'square', 'star', 'triangle', 'wye'];
    return series.map((item, index) => ({
      ...item,
      color: item.color || defaultColors[index % defaultColors.length],
      shape: item.shape || shapes[index % shapes.length] as 'circle',
      opacity: item.opacity !== undefined ? item.opacity : 0.8
    }));
  }, [series, defaultColors]);

  // Calculate domain from data if not provided
  const calculatedDomains = useMemo(() => {
    let xMin = Infinity, xMax = -Infinity, yMin = Infinity, yMax = -Infinity;
    
    series.forEach(serie => {
      serie.data.forEach(point => {
        xMin = Math.min(xMin, point.x);
        xMax = Math.max(xMax, point.x);
        yMin = Math.min(yMin, point.y);
        yMax = Math.max(yMax, point.y);
      });
    });
    
    // Add a 10% margin to the domains
    const xMargin = (xMax - xMin) * 0.1;
    const yMargin = (yMax - yMin) * 0.1;
    
    return {
      xDomain: [xMin - xMargin, xMax + xMargin],
      yDomain: [yMin - yMargin, yMax + yMargin]
    };
  }, [series]);

  // Use provided domains or calculated ones
  const effectiveXDomain = xDomain || calculatedDomains.xDomain;
  const effectiveYDomain = yDomain || calculatedDomains.yDomain;

  return (
    <ChartContainer {...containerProps}>
      <ResponsiveContainer width="100%" height="100%">
        <RechartsScatterChart
          margin={{ top: 20, right: 30, left: 20, bottom: 20 }}
        >
          {showGrid && <CartesianGrid strokeDasharray="3 3" />}
          
          <XAxis 
            type="number" 
            dataKey="x" 
            name={xAxisLabel || "X"} 
            domain={effectiveXDomain}
            label={xAxisLabel ? { value: xAxisLabel, position: 'insideBottom', offset: -5 } : undefined}
          />
          
          <YAxis 
            type="number" 
            dataKey="y" 
            name={yAxisLabel || "Y"} 
            domain={effectiveYDomain}
            label={yAxisLabel ? { value: yAxisLabel, position: 'insideLeft', angle: -90 } : undefined}
          />
          
          {/* Conditionally add ZAxis for bubble charts */}
          {bubble && <ZAxis type="number" dataKey="z" range={zAxisRange} />}
          
          <Tooltip 
            cursor={{ strokeDasharray: '3 3' }}
            formatter={(value, name) => {
              return [`${value}`, name];
            }}
            labelFormatter={(label) => {
              const point = label as unknown as ScatterDataPoint;
              return `(${point.x}, ${point.y})${point.z ? `, Size: ${point.z}` : ''}`;
            }}
          />
          
          <Legend />
          
          {seriesWithColors.map((item, index) => (
            <Scatter
              key={`scatter-${index}`}
              name={item.name}
              data={item.data}
              fill={item.color}
              shape={item.shape}
              fillOpacity={item.opacity}
            />
          ))}
        </RechartsScatterChart>
      </ResponsiveContainer>
    </ChartContainer>
  );
};

export default ScatterPlot; 