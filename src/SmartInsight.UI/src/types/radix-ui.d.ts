// Type definitions for @radix-ui packages

declare module '@radix-ui/react-select' {
  import * as React from 'react';

  // Common props
  interface DOMProps {
    asChild?: boolean;
  }

  // Root components
  export const Root: React.FC<any>;
  export const Group: React.FC<any>;
  export const Value: React.FC<any>;
  export const Trigger: React.FC<any>;
  export const Content: React.FC<any>;
  export const ScrollUpButton: React.FC<any>;
  export const ScrollDownButton: React.FC<any>;
  export const Viewport: React.FC<any>;
  export const Label: React.FC<any>;
  export const Item: React.FC<any>;
  export const ItemText: React.FC<any>;
  export const ItemIndicator: React.FC<any>;
  export const Separator: React.FC<any>;
  export const Portal: React.FC<any>;
}

declare module '@radix-ui/react-slot' {
  import * as React from 'react';
  
  export const Slot: React.FC<any>;
} 