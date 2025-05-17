declare module '*/TenantForm' {
  import React from 'react';
  
  interface TenantFormProps {
    tenant: any;
    onSubmit: (data: any) => Promise<boolean>;
    onCancel: () => void;
    isOpen?: boolean;
  }
  
  const TenantForm: React.FC<TenantFormProps>;
  
  export default TenantForm;
}

declare module '*/TenantConfig' {
  import React from 'react';
  
  interface TenantConfigProps {
    tenantId: string;
    onClose: () => void;
  }
  
  const TenantConfig: React.FC<TenantConfigProps>;
  
  export default TenantConfig;
}

declare module '*/TenantSecuritySettings' {
  import React from 'react';
  
  interface TenantSecuritySettingsProps {
    tenantId: string;
    onClose: () => void;
  }
  
  const TenantSecuritySettings: React.FC<TenantSecuritySettingsProps>;
  
  export default TenantSecuritySettings;
}

declare module '*/DeleteConfirmation' {
  import React from 'react';
  
  interface DeleteConfirmationProps {
    title: string;
    message: string;
    onConfirm: () => void;
    onCancel: () => void;
  }
  
  const DeleteConfirmation: React.FC<DeleteConfirmationProps>;
  
  export default DeleteConfirmation;
}

declare module '*/RoleAssignment' {
  import React from 'react';
  
  interface RoleAssignmentProps {
    onClose: () => void;
  }
  
  const RoleAssignment: React.FC<RoleAssignmentProps>;
  
  export default RoleAssignment;
} 