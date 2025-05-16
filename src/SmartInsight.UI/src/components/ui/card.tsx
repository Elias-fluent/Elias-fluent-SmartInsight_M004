import * as React from "react";

import { cn } from "../../lib/utils";

interface CardProps extends React.HTMLAttributes<HTMLDivElement> {
  /** Optional card title for screen readers (if visual title is different/not present) */
  "aria-label"?: string;
}

const Card = React.forwardRef<HTMLDivElement, CardProps>(
  ({ className, "aria-label": ariaLabel, role, ...props }, ref) => (
    <div
      ref={ref}
      className={cn(
        "rounded-lg border bg-card text-card-foreground shadow-sm",
        className
      )}
      // Add role="region" if not already specified to improve semantics
      role={role || "region"}
      // Add an accessible label for the card if provided
      aria-label={ariaLabel}
      {...props}
    />
  )
);
Card.displayName = "Card";

interface CardHeaderProps extends React.HTMLAttributes<HTMLDivElement> {
  /**
   * Whether this header contains the primary heading for the card
   * Used to adjust semantic markup
   */
  isPrimary?: boolean;
}

const CardHeader = React.forwardRef<HTMLDivElement, CardHeaderProps>(
  ({ className, isPrimary, ...props }, ref) => (
    <div
      ref={ref}
      className={cn("flex flex-col space-y-1.5 p-6", className)}
      // If this is the primary header, mark it as a header for screen readers
      role={isPrimary ? "heading" : undefined}
      aria-level={isPrimary ? 2 : undefined}
      {...props}
    />
  )
);
CardHeader.displayName = "CardHeader";

interface CardTitleProps extends React.HTMLAttributes<HTMLHeadingElement> {
  /** Heading level for semantic structure (default: h3) */
  as?: 'h1' | 'h2' | 'h3' | 'h4' | 'h5' | 'h6';
}

const CardTitle = React.forwardRef<
  HTMLParagraphElement,
  CardTitleProps
>(({ className, as: Heading = 'h3', ...props }, ref) => (
  <Heading
    ref={ref}
    className={cn(
      "text-lg font-semibold leading-none tracking-tight",
      className
    )}
    {...props}
  />
));
CardTitle.displayName = "CardTitle";

const CardDescription = React.forwardRef<
  HTMLParagraphElement,
  React.HTMLAttributes<HTMLParagraphElement>
>(({ className, ...props }, ref) => (
  <p
    ref={ref}
    className={cn("text-sm text-muted-foreground", className)}
    // Indicate that this is descriptive text for the parent container
    id={props.id || undefined}
    {...props}
  />
));
CardDescription.displayName = "CardDescription";

interface CardContentProps extends React.HTMLAttributes<HTMLDivElement> {
  /** If this content should be labeled by the card title for accessibility */
  labeledBy?: string;
}

const CardContent = React.forwardRef<HTMLDivElement, CardContentProps>(
  ({ className, labeledBy, ...props }, ref) => (
    <div 
      ref={ref} 
      className={cn("p-6 pt-0", className)}
      // Link content to the card title if provided
      aria-labelledby={labeledBy}
      {...props} 
    />
  )
);
CardContent.displayName = "CardContent";

const CardFooter = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement>
>(({ className, ...props }, ref) => (
  <div
    ref={ref}
    className={cn("flex items-center p-6 pt-0", className)}
    // Add contentinfo role to better describe this area
    role="contentinfo"
    {...props}
  />
));
CardFooter.displayName = "CardFooter";

export { Card, CardHeader, CardFooter, CardTitle, CardDescription, CardContent }; 