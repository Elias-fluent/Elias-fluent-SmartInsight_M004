import * as React from "react";
import { Controller, type FieldPath, type FieldValues, type UseFormReturn } from "react-hook-form";

import { cn } from "../../lib/utils";
import { Label } from "./label";

// A simpler Form component that doesn't try to handle submit directly
interface FormProps<TFieldValues extends FieldValues> extends React.FormHTMLAttributes<HTMLFormElement> {
  form: UseFormReturn<TFieldValues>;
}

const Form = <TFieldValues extends FieldValues>({
  form,
  className,
  children,
  ...props
}: FormProps<TFieldValues>) => {
  return (
    <form
      className={className}
      {...props}
    >
      {children}
    </form>
  );
};
Form.displayName = "Form";

const FormItem = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement>
>(({ className, ...props }, ref) => (
  <div
    ref={ref}
    className={cn("space-y-2", className)}
    {...props}
  />
));
FormItem.displayName = "FormItem";

const FormLabel = React.forwardRef<
  HTMLLabelElement,
  React.LabelHTMLAttributes<HTMLLabelElement> & {
    required?: boolean;
  }
>(({ className, required, children, ...props }, ref) => (
  <Label
    ref={ref}
    className={cn(required && "after:content-['*'] after:ml-0.5 after:text-red-500", className)}
    {...props}
  >
    {children}
  </Label>
));
FormLabel.displayName = "FormLabel";

const FormControl = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement>
>(({ ...props }, ref) => (
  <div ref={ref} {...props} />
));
FormControl.displayName = "FormControl";

const FormDescription = React.forwardRef<
  HTMLParagraphElement,
  React.HTMLAttributes<HTMLParagraphElement>
>(({ className, ...props }, ref) => (
  <p
    ref={ref}
    className={cn("text-[0.8rem] text-muted-foreground", className)}
    {...props}
  />
));
FormDescription.displayName = "FormDescription";

const FormMessage = React.forwardRef<
  HTMLParagraphElement,
  React.HTMLAttributes<HTMLParagraphElement> & {
    name?: string;
    form?: UseFormReturn<any>;
  }
>(({ className, name, form, children, ...props }, ref) => {
  const error = name && form ? form.formState.errors[name] : undefined;
  const body = error ? String(error.message) : children;

  if (!body) {
    return null;
  }

  return (
    <p
      ref={ref}
      className={cn("text-[0.8rem] font-medium text-destructive", className)}
      {...props}
    >
      {body}
    </p>
  );
});
FormMessage.displayName = "FormMessage";

// Simplified FormField to avoid TypeScript errors
interface FormFieldProps<
  TFieldValues extends FieldValues = FieldValues,
  TName extends FieldPath<TFieldValues> = FieldPath<TFieldValues>
> {
  control: UseFormReturn<TFieldValues>["control"];
  name: TName;
  render: (props: { field: { 
    value: any;
    onChange: (value: any) => void;
    onBlur: () => void;
    name: string;
    ref: React.RefCallback<any>;
  } }) => React.ReactElement;
}

const FormField = <
  TFieldValues extends FieldValues = FieldValues,
  TName extends FieldPath<TFieldValues> = FieldPath<TFieldValues>
>({
  control,
  name,
  render,
}: FormFieldProps<TFieldValues, TName>) => {
  return (
    <Controller
      control={control}
      name={name}
      render={({ field }) => render({ field })}
    />
  );
};
FormField.displayName = "FormField";

export {
  Form,
  FormItem,
  FormLabel,
  FormControl,
  FormDescription,
  FormMessage,
  FormField,
}; 