import * as React from "react";

import { cn } from "../../lib/utils";
import { useFocusVisible } from "../../hooks/useFocusVisible";

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  /** Optional error message to display */
  error?: string;
  /** Whether the input has an error state */
  hasError?: boolean;
  /** ID for the error message element (for aria-errormessage) */
  errorId?: string;
  /** If true, treats a value of "" as invalid */
  required?: boolean;
}

const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, type, error, hasError, errorId, required, "aria-describedby": ariaDescribedby, ...props }, ref) => {
    const { isFocusVisible } = useFocusVisible();
    const hasErrorState = hasError || !!error;
    const errorMessageId = errorId || (error ? `${props.id}-error` : undefined);
    
    // Generate accessibility attributes
    const a11yProps: React.AriaAttributes = {
      // Add aria-invalid for error state
      "aria-invalid": hasErrorState ? true : undefined,
      // Link to error message ID if present
      "aria-errormessage": hasErrorState ? errorMessageId : undefined,
      // Combine with any existing aria-describedby
      "aria-describedby": [ariaDescribedby, hasErrorState ? errorMessageId : undefined]
        .filter(Boolean)
        .join(' ') || undefined,
      // Add required attribute for screen readers
      "aria-required": required ? true : undefined,
    };

    return (
      <input
        type={type}
        className={cn(
          "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50",
          hasErrorState && "border-destructive focus-visible:ring-destructive",
          isFocusVisible && "focus-visible",
          className
        )}
        ref={ref}
        required={required}
        {...a11yProps}
        {...props}
      />
    );
  }
);
Input.displayName = "Input";

export { Input }; 