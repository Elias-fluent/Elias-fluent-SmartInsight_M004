import React, { useState } from 'react';
import {
  ChartContainer,
  BarChart,
  LineChart,
  PieChart,
  ScatterPlot,
  DataTable,
  ChartOptions,
} from './index';
import type { ChartDisplayOptions } from './index';

const ChartExamples: React.FC = () => {
  // Sample data for the charts
  const barData = [
    { name: 'Jan', value: 4000, value2: 2400, value3: 1800 },
    { name: 'Feb', value: 3000, value2: 1398, value3: 2800 },
    { name: 'Mar', value: 2000, value2: 9800, value3: 3200 },
    { name: 'Apr', value: 2780, value2: 3908, value3: 1200 },
    { name: 'May', value: 1890, value2: 4800, value3: 2800 },
    { name: 'Jun', value: 2390, value2: 3800, value3: 1700 },
    { name: 'Jul', value: 3490, value2: 4300, value3: 2200 },
  ];

  const lineData = [
    { date: 'Jan', visitors: 4000, pageViews: 2400 },
    { date: 'Feb', visitors: 3000, pageViews: 1398 },
    { date: 'Mar', visitors: 2000, pageViews: 9800 },
    { date: 'Apr', visitors: 2780, pageViews: 3908 },
    { date: 'May', visitors: 1890, pageViews: 4800 },
    { date: 'Jun', visitors: 2390, pageViews: 3800 },
    { date: 'Jul', visitors: 3490, pageViews: 4300 },
  ];

  const pieData = [
    { name: 'Group A', value: 400 },
    { name: 'Group B', value: 300 },
    { name: 'Group C', value: 300 },
    { name: 'Group D', value: 200 },
    { name: 'Group E', value: 100 },
  ];

  const scatterData = [
    { name: 'Series 1', data: [
      { x: 100, y: 200, z: 40 },
      { x: 120, y: 100, z: 20 },
      { x: 170, y: 300, z: 30 },
      { x: 140, y: 250, z: 35 },
      { x: 150, y: 400, z: 50 },
      { x: 110, y: 280, z: 25 },
    ]},
    { name: 'Series 2', data: [
      { x: 200, y: 260, z: 30 },
      { x: 240, y: 290, z: 40 },
      { x: 190, y: 290, z: 45 },
      { x: 198, y: 250, z: 35 },
      { x: 180, y: 280, z: 25 },
      { x: 210, y: 220, z: 20 },
    ]}
  ];

  const tableData = barData.map((item, index) => ({
    id: index,
    month: item.name,
    value1: item.value,
    value2: item.value2,
    value3: item.value3,
    total: item.value + item.value2 + item.value3
  }));

  // Define the columns for the data table
  const tableColumns = [
    {
      accessorKey: 'month',
      header: 'Month'
    },
    {
      accessorKey: 'value1',
      header: 'Value 1'
    },
    {
      accessorKey: 'value2',
      header: 'Value 2'
    },
    {
      accessorKey: 'value3',
      header: 'Value 3'
    },
    {
      accessorKey: 'total',
      header: 'Total'
    }
  ];

  // State for chart display options
  const [displayOptions, setDisplayOptions] = useState<ChartDisplayOptions>({
    showLegend: true,
    showGrid: true,
    showTooltip: true,
    showDataLabels: false
  });

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-4 p-4">
      <div className="space-y-4">
        <h2 className="text-xl font-bold">Bar Chart</h2>
        <BarChart
          title="Monthly Revenue"
          description="Comparison of monthly revenue sources"
          data={barData}
          xAxisDataKey="name"
          xAxisLabel="Month"
          yAxisLabel="Revenue ($)"
          series={[
            { dataKey: 'value', name: 'Product A' },
            { dataKey: 'value2', name: 'Product B' },
            { dataKey: 'value3', name: 'Product C' },
          ]}
          height={300}
        />

        <h2 className="text-xl font-bold mt-8">Line Chart</h2>
        <LineChart
          title="Website Analytics"
          description="Monthly visitors and page views"
          data={lineData}
          xAxisDataKey="date"
          xAxisLabel="Month"
          yAxisLabel="Count"
          series={[
            { dataKey: 'visitors', name: 'Unique Visitors', strokeWidth: 3 },
            { dataKey: 'pageViews', name: 'Page Views', color: '#FF8042' }
          ]}
          height={300}
        />
      </div>

      <div className="space-y-4">
        <h2 className="text-xl font-bold">Pie Chart</h2>
        <PieChart
          title="Market Distribution"
          description="Share by customer segments"
          data={pieData}
          donut={true}
          height={300}
        />

        <h2 className="text-xl font-bold mt-8">Scatter Plot</h2>
        <ScatterPlot
          title="Product Comparison"
          description="Price vs. Performance with size as market share"
          series={scatterData}
          xAxisLabel="Price ($)"
          yAxisLabel="Performance Score"
          bubble={true}
          height={300}
        />
      </div>

      <div className="col-span-1 md:col-span-2">
        <h2 className="text-xl font-bold">Data Table</h2>
        <ChartOptions
          onDisplayOptionsChange={setDisplayOptions}
          displayOptions={displayOptions}
          className="mb-4"
        />
        <DataTable
          title="Revenue Data"
          description="Monthly revenue breakdown by product"
          data={tableData}
          columns={tableColumns}
          height={400}
          pageSize={5}
          searchable={true}
          sortable={true}
        />
      </div>
    </div>
  );
};

export default ChartExamples; 