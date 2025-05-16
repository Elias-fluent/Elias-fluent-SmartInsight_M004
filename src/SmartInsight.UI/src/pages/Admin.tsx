import React, { useState } from 'react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../components/ui/tabs';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import DataSourceList from '../components/admin/DataSourceList';
import { useAuth } from '../hooks/useAuth';
import { Alert, AlertDescription, AlertTitle } from '../components/ui/alert';
import { AlertTriangle } from 'lucide-react';

const Admin: React.FC = () => {
  const [activeTab, setActiveTab] = useState('data-sources');
  const { user } = useAuth();

  // Check if user has admin role
  const isAdmin = user?.roles?.includes('Admin');

  if (!isAdmin) {
    return (
      <div className="container mx-auto py-8">
        <Alert variant="destructive" className="mb-6">
          <AlertTriangle className="h-4 w-4" />
          <AlertTitle>Unauthorized</AlertTitle>
          <AlertDescription>
            You don't have permission to access the Admin panel.
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
        defaultValue="data-sources"
        value={activeTab}
        onValueChange={setActiveTab}
        className="space-y-4"
      >
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="data-sources">Data Sources</TabsTrigger>
          <TabsTrigger value="users">Users</TabsTrigger>
          <TabsTrigger value="ingestion">Ingestion Jobs</TabsTrigger>
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
              <p>User management will be implemented in a future update.</p>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="ingestion" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Ingestion Jobs</CardTitle>
              <CardDescription>
                Monitor and manage data ingestion tasks.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <p>Ingestion job monitoring will be implemented in a future update.</p>
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