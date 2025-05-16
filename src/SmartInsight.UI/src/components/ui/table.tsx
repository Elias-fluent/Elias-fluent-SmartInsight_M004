import * as React from "react";

import { cn } from "../../lib/utils";

interface TableProps extends React.HTMLAttributes<HTMLTableElement> {
  /** Provides a summary of the table's purpose and structure for screen readers */
  summary?: string;
}

const Table = React.forwardRef<
  HTMLTableElement,
  TableProps
>(({ className, summary, ...props }, ref) => (
  <div className="w-full overflow-auto">
    <table
      ref={ref}
      className={cn("w-full caption-bottom text-sm", className)}
      // Add optional summary for complex tables
      summary={summary}
      // Add role explicitly for clarity (though table has this role implicitly)
      role="table"
      {...props}
    />
  </div>
));
Table.displayName = "Table";

interface TableHeaderProps extends React.HTMLAttributes<HTMLTableSectionElement> {
  /** Whether to announce sort order changes for screen readers */
  announceSortChange?: boolean;
}

const TableHeader = React.forwardRef<
  HTMLTableSectionElement,
  TableHeaderProps
>(({ className, announceSortChange, ...props }, ref) => (
  <thead 
    ref={ref} 
    className={cn("[&_tr]:border-b", className)} 
    // Explicitly set role for better semantics
    role="rowgroup"
    // Add live region for sort order announcements if enabled
    aria-live={announceSortChange ? "polite" : undefined}
    {...props} 
  />
));
TableHeader.displayName = "TableHeader";

const TableBody = React.forwardRef<
  HTMLTableSectionElement,
  React.HTMLAttributes<HTMLTableSectionElement>
>(({ className, ...props }, ref) => (
  <tbody
    ref={ref}
    className={cn("[&_tr:last-child]:border-0", className)}
    role="rowgroup"
    {...props}
  />
));
TableBody.displayName = "TableBody";

const TableFooter = React.forwardRef<
  HTMLTableSectionElement,
  React.HTMLAttributes<HTMLTableSectionElement>
>(({ className, ...props }, ref) => (
  <tfoot
    ref={ref}
    className={cn("bg-primary font-medium text-primary-foreground", className)}
    role="rowgroup"
    {...props}
  />
));
TableFooter.displayName = "TableFooter";

interface TableRowProps extends React.HTMLAttributes<HTMLTableRowElement> {
  /** Whether this row is a header row */
  isHeaderRow?: boolean;
  /** Whether this row is selected */
  selected?: boolean;
}

const TableRow = React.forwardRef<
  HTMLTableRowElement,
  TableRowProps
>(({ className, isHeaderRow, selected, ...props }, ref) => (
  <tr
    ref={ref}
    className={cn(
      "border-b transition-colors hover:bg-muted/50 data-[state=selected]:bg-muted",
      selected && "data-[state=selected]", // Apply selection state class
      className
    )}
    role="row"
    aria-selected={selected}
    // Add appropriate scope for header rows
    {...(isHeaderRow && { scope: "colgroup" })}
    {...props}
  />
));
TableRow.displayName = "TableRow";

interface TableHeadProps extends React.ThHTMLAttributes<HTMLTableCellElement> {
  /** Sorting direction if this column is sortable */
  sortDirection?: 'asc' | 'desc' | null;
}

const TableHead = React.forwardRef<
  HTMLTableCellElement,
  TableHeadProps
>(({ className, sortDirection, onClick, ...props }, ref) => {
  // Determine if this is a sortable header
  const isSortable = !!onClick;
  
  return (
    <th
      ref={ref}
      className={cn(
        "h-12 px-4 text-left align-middle font-medium text-muted-foreground [&:has([role=checkbox])]:pr-0",
        isSortable && "cursor-pointer select-none",
        className
      )}
      role="columnheader"
      scope="col"
      aria-sort={sortDirection === 'asc' 
        ? 'ascending' 
        : sortDirection === 'desc' 
          ? 'descending' 
          : sortDirection === null 
            ? 'none' 
            : undefined}
      // If sortable, add button role and tabindex for keyboard accessibility
      tabIndex={isSortable ? 0 : undefined}
      // Handle keyboard activation for sortable headers
      onKeyDown={isSortable 
        ? (e) => {
            if (e.key === 'Enter' || e.key === ' ') {
              e.preventDefault();
              onClick?.(e as any);
            }
          } 
        : undefined}
      {...props}
    />
  );
});
TableHead.displayName = "TableHead";

interface TableCellProps extends React.TdHTMLAttributes<HTMLTableCellElement> {
  /** Whether this cell is a header cell for a row */
  isRowHeader?: boolean;
}

const TableCell = React.forwardRef<
  HTMLTableCellElement,
  TableCellProps
>(({ className, isRowHeader, ...props }, ref) => {
  // Determine if we should render as th instead of td
  const Component = isRowHeader ? 'th' : 'td';
  
  return (
    <Component
      ref={ref}
      className={cn("p-4 align-middle [&:has([role=checkbox])]:pr-0", className)}
      // If it's a row header, set the appropriate scope
      {...(isRowHeader && { scope: "row" })}
      {...props}
    />
  );
});
TableCell.displayName = "TableCell";

interface TableCaptionProps extends React.HTMLAttributes<HTMLTableCaptionElement> {
  /** Whether this caption should be hidden visually but available to screen readers */
  visuallyHidden?: boolean;
}

const TableCaption = React.forwardRef<
  HTMLTableCaptionElement,
  TableCaptionProps
>(({ className, visuallyHidden, ...props }, ref) => (
  <caption
    ref={ref}
    className={cn(
      "mt-4 text-sm text-muted-foreground",
      visuallyHidden && "sr-only", // Hide visually but keep for screen readers
      className
    )}
    {...props}
  />
));
TableCaption.displayName = "TableCaption";

export {
  Table,
  TableHeader,
  TableBody,
  TableFooter,
  TableHead,
  TableRow,
  TableCell,
  TableCaption,
}; 