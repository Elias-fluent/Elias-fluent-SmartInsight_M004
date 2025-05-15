import React from 'react';
import {  PieChart as RechartsPieChart,  Pie,  Cell,  Tooltip,  Legend,  ResponsiveContainer} from 'recharts';import type { PieProps } from 'recharts';
import { ChartContainer, type ChartContainerProps } from './ChartContainer';

export interface PieChartDataItem {
  name: string;
  value: number;
  color?: string;
}

export interface PieChartProps extends Omit<ChartContainerProps, 'children'> {
  data: PieChartDataItem[];
  dataKey?: string;
  nameKey?: string;
  innerRadius?: number | string;
  outerRadius?: number | string;
  paddingAngle?: number;
  startAngle?: number;
  endAngle?: number;
  labelLine?: boolean;
  label?: boolean | PieProps['label'];
  donut?: boolean;
}

/**
 * Pie Chart component that wraps Recharts PieChart with our standard styling and features.
 */
export const PieChart: React.FC<PieChartProps> = ({
  data,
  dataKey = 'value',
  nameKey = 'name',
  innerRadius = 0,
  outerRadius = '80%',
  paddingAngle = 0,
  startAngle = 0,
  endAngle = 360,
  labelLine = true,
  label = true,
  donut = false,
  ...containerProps
}) => {
  // Default colors for pie slices if not specified
  const defaultColors = [
    '#0088FE', '#00C49F', '#FFBB28', '#FF8042', 
    '#8884D8', '#82CA9D', '#FFC658', '#8DD1E1',
    '#A4DE6C', '#D0ED57', '#FFC658', '#FA8072'
  ];

  // Calculate inner radius for donut chart
  const effectiveInnerRadius = donut ? (typeof outerRadius === 'string' ? '50%' : outerRadius * 0.6) : innerRadius;

  // Custom label formatter that shows name and percentage
  const renderCustomizedLabel = (props: any) => {
    const { cx, cy, midAngle, innerRadius, outerRadius, percent } = props;
    const radius = innerRadius + (outerRadius - innerRadius) * 0.5;
    const x = cx + radius * Math.cos(-midAngle * Math.PI / 180);
    const y = cy + radius * Math.sin(-midAngle * Math.PI / 180);
    
    // Only show label if the slice is big enough (greater than 5%)
    if (percent < 0.05) return null;
    
    return (
      <text 
        x={x} 
        y={y} 
        fill="white" 
        textAnchor="middle" 
        dominantBaseline="central"
        fontSize={12}
      >
        {`${(percent * 100).toFixed(0)}%`}
      </text>
    );
  };

  return (
    <ChartContainer {...containerProps}>
      <ResponsiveContainer width="100%" height="100%">
        <RechartsPieChart margin={{ top: 20, right: 30, left: 20, bottom: 20 }}>
          <Pie
            data={data}
            dataKey={dataKey}
            nameKey={nameKey}
            cx="50%"
            cy="50%"
            innerRadius={effectiveInnerRadius}
            outerRadius={outerRadius}
            paddingAngle={paddingAngle}
            startAngle={startAngle}
            endAngle={endAngle}
            labelLine={labelLine}
            label={label === true ? renderCustomizedLabel : label}
            isAnimationActive={true}
          >
            {data.map((entry, index) => (
              <Cell 
                key={`cell-${index}`} 
                fill={entry.color || defaultColors[index % defaultColors.length]} 
              />
            ))}
          </Pie>
          <Tooltip 
            formatter={(value: number) => [
              `${value.toLocaleString()}`, 
              'Value'
            ]}
          />
          <Legend />
        </RechartsPieChart>
      </ResponsiveContainer>
    </ChartContainer>
  );
};

export default PieChart; 