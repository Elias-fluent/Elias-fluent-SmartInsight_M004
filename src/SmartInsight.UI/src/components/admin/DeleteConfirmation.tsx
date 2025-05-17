import React from 'react';
import { ConfirmationDialog } from '../ui/confirmation-dialog';

interface DeleteConfirmationProps {
  title: string;
  message: string;
  onConfirm: () => void;
  onCancel: () => void;
}

const DeleteConfirmation: React.FC<DeleteConfirmationProps> = ({
  title,
  message,
  onConfirm,
  onCancel,
}) => {
  return (
    <ConfirmationDialog
      open={true}
      onOpenChange={(open) => !open && onCancel()}
      title={title}
      description={message}
      confirmLabel="Delete"
      variant="destructive"
      onConfirm={onConfirm}
    />
  );
};

export default DeleteConfirmation; 