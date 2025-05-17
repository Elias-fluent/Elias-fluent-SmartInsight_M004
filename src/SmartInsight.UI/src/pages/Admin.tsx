import React, { useState, useEffect } from 'react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../components/ui/tabs';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import DataSourceList from '../components/admin/DataSourceList';
import UserList from '../components/admin/UserList';
import TenantList from '../components/admin/TenantList';
import { useAuth } from '../hooks/useAuth';
import { Alert, AlertDescription, AlertTitle } from '../components/ui/alert';
import { AlertTriangle } from 'lucide-react';

const Admin: React.FC = () => {
  const [activeTab, setActiveTab] = useState('users');
  const { user, hasRole, isAuthenticated } = useAuth();

  // For debugging purposes
  useEffect(() => {
    console.log('Auth state:', { user, isAuthenticated, roles: user?.roles });
  }, [user, isAuthenticated]);

  // Use just 'Admin' role to match MockAuth.js
  const isAdmin = hasRole('Admin');

  if (!isAuthenticated) {
    return (
      <div className="container mx-auto py-8">
        <Alert variant="destructive" className="mb-6">
          <AlertTriangle className="h-4 w-4" />
          <AlertTitle>Not Authenticated</AlertTitle>
          <AlertDescription>
            You need to log in to access the Admin panel.
          </AlertDescription>
        </Alert>
      </div>
    );
  }

  if (!isAdmin) {
    return (
      <div className="container mx-auto py-8">
        <Alert variant="destructive" className="mb-6">
          <AlertTriangle className="h-4 w-4" />
          <AlertTitle>Unauthorized</AlertTitle>
          <AlertDescription>
            You don't have permission to access the Admin panel. Your roles: {user?.roles?.join(', ')}
          </AlertDescription>
        </Alert>
      </div>
    );
  }

  return (
    <div className="container mx-auto py-8">
      <div className="mb-8">
        <h1 className="text-3xl font-bold tracking-tight">Admin Panel</h1>
        <p className="text-muted-foreground">
          Manage system configuration, data sources, and users.
        </p>
      </div>

      <Tabs
        defaultValue="users"
        value={activeTab}
        onValueChange={setActiveTab}
        className="space-y-4"
      >
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="data-sources">Data Sources</TabsTrigger>
          <TabsTrigger value="users">Users</TabsTrigger>
          <TabsTrigger value="tenants">Tenants</TabsTrigger>
          <TabsTrigger value="system">System Settings</TabsTrigger>
        </TabsList>

        <TabsContent value="data-sources" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Data Sources</CardTitle>
              <CardDescription>
                Configure and manage connections to external data sources.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <DataSourceList />
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="users" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>User Management</CardTitle>
              <CardDescription>
                Manage users, roles, and permissions.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <UserList />
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="tenants" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Tenant Management</CardTitle>
              <CardDescription>
                Manage organization tenants and their settings.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <TenantList />
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="system" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>System Settings</CardTitle>
              <CardDescription>
                Configure system-wide settings and view metrics.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <p>System settings will be implemented in a future update.</p>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default Admin; 