// Type definitions for @tanstack/react-table

declare module '@tanstack/react-table' {
  export function flexRender(Comp: any, props: any): React.ReactNode;
  export function getCoreRowModel(): any;
  export function getSortedRowModel(): any;
  export function getPaginationRowModel(): any;
  export function getFilteredRowModel(): any;
  export function useReactTable(options: any): any;
  export type ColumnDef<TData extends object> = any;
  export type SortingState = any;
} 