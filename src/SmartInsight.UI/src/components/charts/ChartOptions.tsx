import React from 'react';

export interface ChartThemeOption {
  name: string;
  colors: string[];
  background?: string;
  textColor?: string;
  gridColor?: string;
  tooltipBackground?: string;
  tooltipTextColor?: string;
}

export interface ChartExportOption {
  format: 'png' | 'jpeg' | 'svg' | 'csv' | 'excel';
  label: string;
  icon?: React.ReactNode;
}

export interface ChartDisplayOptions {
  showLegend?: boolean;
  showGrid?: boolean;
  showTooltip?: boolean;
  showDataLabels?: boolean;
  animation?: boolean;
  responsiveHeight?: boolean;
}

export interface ChartOptionsProps {
  onThemeChange?: (theme: ChartThemeOption) => void;
  onExport?: (format: ChartExportOption) => void;
  onDisplayOptionsChange?: (options: ChartDisplayOptions) => void;
  onRefresh?: () => void;
  availableThemes?: ChartThemeOption[];
  availableExportFormats?: ChartExportOption[];
  currentTheme?: string;
  displayOptions?: ChartDisplayOptions;
  className?: string;
  position?: 'top' | 'bottom' | 'left' | 'right';
}

/**
 * A component for chart customization options that can be applied to any chart.
 */
export const ChartOptions: React.FC<ChartOptionsProps> = ({
  onThemeChange,
  onExport,
  onDisplayOptionsChange,
  onRefresh,
  availableThemes = defaultThemes,
  availableExportFormats = defaultExportFormats,
  currentTheme = 'default',
  displayOptions = defaultDisplayOptions,
  className = '',
  position = 'top'
}) => {
  const handleThemeChange = (themeName: string) => {
    const theme = availableThemes.find(t => t.name === themeName);
    if (theme && onThemeChange) {
      onThemeChange(theme);
    }
  };

  const handleExport = (format: ChartExportOption) => {
    if (onExport) {
      onExport(format);
    }
  };

  const handleDisplayOptionChange = (optionName: keyof ChartDisplayOptions, value: boolean) => {
    if (onDisplayOptionsChange) {
      onDisplayOptionsChange({
        ...displayOptions,
        [optionName]: value
      });
    }
  };

  const positionClasses = {
    top: 'flex-row space-x-2 mb-2',
    bottom: 'flex-row space-x-2 mt-2',
    left: 'flex-col space-y-2 mr-2',
    right: 'flex-col space-y-2 ml-2'
  };

  return (
    <div className={`flex ${positionClasses[position]} ${className}`}>
      {/* Theme selector */}
      {onThemeChange && (
        <div className="relative inline-block">
          <select
            value={currentTheme}
            onChange={(e) => handleThemeChange(e.target.value)}
            className="block w-full p-2 pr-8 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent"
          >
            {availableThemes.map((theme) => (
              <option key={theme.name} value={theme.name}>
                {theme.name}
              </option>
            ))}
          </select>
          <div className="absolute inset-y-0 right-0 flex items-center pr-2 pointer-events-none">
            <svg className="w-5 h-5 text-gray-400" fill="currentColor" viewBox="0 0 20 20">
              <path
                fillRule="evenodd"
                d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 011.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z"
                clipRule="evenodd"
              />
            </svg>
          </div>
        </div>
      )}

      {/* Display options */}
      {onDisplayOptionsChange && (
        <div className="flex space-x-2">
          <label className="inline-flex items-center cursor-pointer">
            <input
              type="checkbox"
              checked={displayOptions.showLegend}
              onChange={(e) => handleDisplayOptionChange('showLegend', e.target.checked)}
              className="sr-only"
            />
            <span className={`px-2 py-1 text-xs rounded ${displayOptions.showLegend ? 'bg-primary-100 text-primary-800' : 'bg-gray-100 text-gray-600'}`}>
              Legend
            </span>
          </label>
          
          <label className="inline-flex items-center cursor-pointer">
            <input
              type="checkbox"
              checked={displayOptions.showGrid}
              onChange={(e) => handleDisplayOptionChange('showGrid', e.target.checked)}
              className="sr-only"
            />
            <span className={`px-2 py-1 text-xs rounded ${displayOptions.showGrid ? 'bg-primary-100 text-primary-800' : 'bg-gray-100 text-gray-600'}`}>
              Grid
            </span>
          </label>
          
          <label className="inline-flex items-center cursor-pointer">
            <input
              type="checkbox"
              checked={displayOptions.showDataLabels}
              onChange={(e) => handleDisplayOptionChange('showDataLabels', e.target.checked)}
              className="sr-only"
            />
            <span className={`px-2 py-1 text-xs rounded ${displayOptions.showDataLabels ? 'bg-primary-100 text-primary-800' : 'bg-gray-100 text-gray-600'}`}>
              Labels
            </span>
          </label>
        </div>
      )}

      {/* Export options */}
      {onExport && (
        <div className="flex space-x-1">
          {availableExportFormats.map((format) => (
            <button
              key={format.format}
              onClick={() => handleExport(format)}
              className="p-1 text-xs bg-gray-100 hover:bg-gray-200 rounded"
              title={`Export as ${format.label}`}
            >
              {format.icon ? (
                format.icon
              ) : (
                <span>{format.label}</span>
              )}
            </button>
          ))}
        </div>
      )}

      {/* Refresh button */}
      {onRefresh && (
        <button
          onClick={onRefresh}
          className="p-1 text-xs bg-gray-100 hover:bg-gray-200 rounded"
          title="Refresh data"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
          </svg>
        </button>
      )}
    </div>
  );
};

// Default themes
const defaultThemes: ChartThemeOption[] = [
  {
    name: 'default',
    colors: ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884D8', '#82CA9D', '#FFC658', '#8DD1E1'],
    background: 'white',
    textColor: '#333333',
    gridColor: '#DDDDDD'
  },
  {
    name: 'dark',
    colors: ['#81A1C1', '#88C0D0', '#8FBCBB', '#A3BE8C', '#B48EAD', '#EBCB8B', '#D08770', '#BF616A'],
    background: '#2E3440',
    textColor: '#ECEFF4',
    gridColor: '#4C566A'
  },
  {
    name: 'pastel',
    colors: ['#FFB5B5', '#FFDAB5', '#FFFFB5', '#B5FFB5', '#B5FFFF', '#B5B5FF', '#FFB5FF', '#E2E2E2'],
    background: 'white',
    textColor: '#555555',
    gridColor: '#EEEEEE'
  }
];

// Default export formats
const defaultExportFormats: ChartExportOption[] = [
  { format: 'png', label: 'PNG' },
  { format: 'svg', label: 'SVG' },
  { format: 'csv', label: 'CSV' }
];

// Default display options
const defaultDisplayOptions: ChartDisplayOptions = {
  showLegend: true,
  showGrid: true,
  showTooltip: true,
  showDataLabels: false,
  animation: true,
  responsiveHeight: true
};

export default ChartOptions; 