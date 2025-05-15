import React, { useMemo } from 'react';
import {
  LineChart as RechartsLineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import { ChartContainer, ChartContainerProps } from './ChartContainer';

export interface LineChartDataItem {
  [key: string]: string | number | Date;
}

export interface LineSeries {
  dataKey: string;
  name?: string;
  color?: string;
  strokeWidth?: number;
  dotSize?: number;
  type?: 'linear' | 'monotone' | 'step' | 'stepBefore' | 'stepAfter';
  connectNulls?: boolean;
}

export interface LineChartProps extends Omit<ChartContainerProps, 'children'> {
  data: LineChartDataItem[];
  series: LineSeries[];
  xAxisDataKey: string;
  xAxisLabel?: string;
  yAxisLabel?: string;
  xAxisType?: 'category' | 'number';
  showGrid?: boolean;
  showDots?: boolean;
  curveType?: 'linear' | 'monotone' | 'step' | 'stepBefore' | 'stepAfter';
  areaChart?: boolean;
}

/**
 * Line Chart component that wraps Recharts LineChart with our standard styling and features.
 */
export const LineChart: React.FC<LineChartProps> = ({
  data,
  series,
  xAxisDataKey,
  xAxisLabel,
  yAxisLabel,
  xAxisType = 'category',
  showGrid = true,
  showDots = true,
  curveType = 'linear',
  areaChart = false,
  ...containerProps
}) => {
  // Default colors for lines if not specified
  const defaultColors = [
    '#0088FE', '#00C49F', '#FFBB28', '#FF8042', 
    '#8884D8', '#82CA9D', '#FFC658', '#8DD1E1'
  ];

  // Apply default colors to series that don't have colors specified
  const seriesWithColors = useMemo(() => {
    return series.map((item, index) => ({
      ...item,
      color: item.color || defaultColors[index % defaultColors.length],
      strokeWidth: item.strokeWidth || 2,
      dotSize: item.dotSize || 5,
      type: item.type || curveType,
      connectNulls: item.connectNulls ?? true
    }));
  }, [series, curveType]);

  return (
    <ChartContainer {...containerProps}>
      <ResponsiveContainer width="100%" height="100%">
        <RechartsLineChart
          data={data}
          margin={{ top: 20, right: 30, left: 20, bottom: 20 }}
        >
          {showGrid && <CartesianGrid strokeDasharray="3 3" />}
          <XAxis 
            dataKey={xAxisDataKey} 
            type={xAxisType}
            label={xAxisLabel ? { value: xAxisLabel, position: 'insideBottom', offset: -5 } : undefined}
          />
          <YAxis 
            label={yAxisLabel ? { value: yAxisLabel, position: 'insideLeft', angle: -90 } : undefined}
          />
          <Tooltip />
          <Legend />
          
          {seriesWithColors.map((item, index) => (
            <Line
              key={`line-${item.dataKey}-${index}`}
              type={item.type}
              dataKey={item.dataKey}
              name={item.name || item.dataKey}
              stroke={item.color}
              strokeWidth={item.strokeWidth}
              dot={showDots ? { r: item.dotSize } : false}
              activeDot={{ r: (item.dotSize || 5) + 2 }}
              connectNulls={item.connectNulls}
              isAnimationActive={true}
            />
          ))}
        </RechartsLineChart>
      </ResponsiveContainer>
    </ChartContainer>
  );
};

export default LineChart; 