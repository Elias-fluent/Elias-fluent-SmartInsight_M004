import React, { useMemo } from 'react';
import {
  BarChart as RechartsBarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  TooltipProps
} from 'recharts';
import { ChartContainer, ChartContainerProps } from './ChartContainer';

export interface BarChartDataItem {
  [key: string]: string | number;
}

export interface BarSeries {
  dataKey: string;
  name?: string;
  color?: string;
  stackId?: string;
}

export interface BarChartProps extends Omit<ChartContainerProps, 'children'> {
  data: BarChartDataItem[];
  series: BarSeries[];
  xAxisDataKey: string;
  xAxisLabel?: string;
  yAxisLabel?: string;
  stacked?: boolean;
  vertical?: boolean;
  showGrid?: boolean;
}

/**
 * Bar Chart component that wraps Recharts BarChart with our standard styling and features.
 */
export const BarChart: React.FC<BarChartProps> = ({
  data,
  series,
  xAxisDataKey,
  xAxisLabel,
  yAxisLabel,
  stacked = false,
  vertical = false,
  showGrid = true,
  ...containerProps
}) => {
  // Default colors for bars if not specified
  const defaultColors = [
    '#0088FE', '#00C49F', '#FFBB28', '#FF8042', 
    '#8884D8', '#82CA9D', '#FFC658', '#8DD1E1'
  ];

  // Apply default colors to series that don't have colors specified
  const seriesWithColors = useMemo(() => {
    return series.map((item, index) => ({
      ...item,
      color: item.color || defaultColors[index % defaultColors.length],
      stackId: stacked ? (item.stackId || 'stack1') : undefined
    }));
  }, [series, stacked]);

  return (
    <ChartContainer {...containerProps}>
      <ResponsiveContainer width="100%" height="100%">
        <RechartsBarChart
          data={data}
          layout={vertical ? 'vertical' : 'horizontal'}
          margin={{ top: 20, right: 30, left: 20, bottom: 20 }}
        >
          {showGrid && <CartesianGrid strokeDasharray="3 3" />}
          
          {/* Swap XAxis and YAxis for vertical layout */}
          {vertical ? (
            <>
              <YAxis dataKey={xAxisDataKey} type="category" label={xAxisLabel ? { value: xAxisLabel, position: 'insideLeft', angle: -90 } : undefined} />
              <XAxis type="number" label={yAxisLabel ? { value: yAxisLabel, position: 'insideBottom', offset: -5 } : undefined} />
            </>
          ) : (
            <>
              <XAxis dataKey={xAxisDataKey} label={xAxisLabel ? { value: xAxisLabel, position: 'insideBottom', offset: -5 } : undefined} />
              <YAxis label={yAxisLabel ? { value: yAxisLabel, position: 'insideLeft', angle: -90 } : undefined} />
            </>
          )}
          
          <Tooltip />
          <Legend />
          
          {seriesWithColors.map((item, index) => (
            <Bar
              key={`bar-${item.dataKey}-${index}`}
              dataKey={item.dataKey}
              name={item.name || item.dataKey}
              fill={item.color}
              stackId={item.stackId}
            />
          ))}
        </RechartsBarChart>
      </ResponsiveContainer>
    </ChartContainer>
  );
};

export default BarChart; 