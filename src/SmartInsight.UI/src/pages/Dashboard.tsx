import React from 'react';
import { Button } from '../components/shadcn/Button';

const Dashboard: React.FC = () => {
  return (
    <div className="space-y-6">
      <div className="border rounded-lg p-4 shadow-sm bg-card">
        <h2 className="text-xl font-semibold mb-4 text-card-foreground">Dashboard</h2>
        
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
          <div className="border rounded-md p-4 bg-background">
            <h3 className="font-medium mb-2">Data Sources</h3>
            <p className="text-sm text-muted-foreground">8 active connections</p>
          </div>
          
          <div className="border rounded-md p-4 bg-background">
            <h3 className="font-medium mb-2">Users</h3>
            <p className="text-sm text-muted-foreground">24 active users</p>
          </div>
          
          <div className="border rounded-md p-4 bg-background">
            <h3 className="font-medium mb-2">Knowledge Base</h3>
            <p className="text-sm text-muted-foreground">1,245 entities indexed</p>
          </div>
        </div>
        
        <div className="flex justify-end space-x-2">
          <Button variant="outline" size="sm">Refresh</Button>
          <Button size="sm">View Details</Button>
        </div>
      </div>
      
      <div className="border rounded-lg p-4 shadow-sm bg-card">
        <h2 className="text-xl font-semibold mb-4 text-card-foreground">Recent Activity</h2>
        <p className="text-center text-muted-foreground py-6">No recent activity to display</p>
        <div className="flex justify-center">
          <Button variant="secondary">Create New Query</Button>
        </div>
      </div>
    </div>
  );
};

export default Dashboard; 