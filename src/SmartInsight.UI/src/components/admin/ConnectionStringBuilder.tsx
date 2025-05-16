import React, { useState, useEffect } from 'react';
import { Input } from '../ui/input';
import { Label } from '../ui/label';
import { Button } from '../ui/button';
import { Card } from '../ui/card';

interface ConnectionStringBuilderProps {
  dataSourceType: number;
  onChange: (value: string) => void;
}

const ConnectionStringBuilder: React.FC<ConnectionStringBuilderProps> = ({
  dataSourceType,
  onChange,
}) => {
  const [server, setServer] = useState('');
  const [port, setPort] = useState('');
  const [database, setDatabase] = useState('');
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [additionalParams, setAdditionalParams] = useState('');
  const [builtString, setBuiltString] = useState('');

  // Get template and placeholder based on data source type
  const getConnectionTemplate = () => {
    switch (dataSourceType) {
      case 1: // SqlServer
        return "Server={server},{port};Database={database};User Id={username};Password={password};{additionalParams}";
      case 2: // PostgreSQL
        return "Host={server};Port={port};Database={database};Username={username};Password={password};{additionalParams}";
      case 3: // MySQL
        return "Server={server};Port={port};Database={database};Uid={username};Pwd={password};{additionalParams}";
      default:
        return "";
    }
  };

  // Get field placeholders based on data source type
  const getFieldPlaceholders = () => {
    switch (dataSourceType) {
      case 1: // SqlServer
        return {
          server: "localhost",
          port: "1433",
          database: "master",
          username: "sa",
          password: "yourpassword",
          additionalParams: "TrustServerCertificate=True;Encrypt=True",
        };
      case 2: // PostgreSQL
        return {
          server: "localhost",
          port: "5432",
          database: "postgres",
          username: "postgres",
          password: "yourpassword",
          additionalParams: "SSL Mode=Prefer;Trust Server Certificate=true",
        };
      case 3: // MySQL
        return {
          server: "localhost",
          port: "3306",
          database: "mysql",
          username: "root",
          password: "yourpassword",
          additionalParams: "SslMode=Required;AllowPublicKeyRetrieval=true",
        };
      default:
        return {
          server: "",
          port: "",
          database: "",
          username: "",
          password: "",
          additionalParams: "",
        };
    }
  };

  // Create connection string from inputs
  const buildConnectionString = () => {
    let template = getConnectionTemplate();
    
    if (!template) return "";
    
    let result = template
      .replace("{server}", server)
      .replace("{port}", port)
      .replace("{database}", database)
      .replace("{username}", username)
      .replace("{password}", password)
      .replace("{additionalParams}", additionalParams);
    
    // Clean up any remaining placeholders and trailing semicolons
    result = result.replace(/\{[^}]*\}/g, "");
    result = result.replace(/;+$/, "");
    result = result.replace(/;;/g, ";");
    
    return result;
  };

  // Update the connection string when inputs change
  useEffect(() => {
    const newConnectionString = buildConnectionString();
    setBuiltString(newConnectionString);
  }, [server, port, database, username, password, additionalParams, dataSourceType]);

  // Set the built connection string to the form
  const applyConnectionString = () => {
    onChange(builtString);
  };

  // Reset the builder fields
  const resetFields = () => {
    setServer('');
    setPort('');
    setDatabase('');
    setUsername('');
    setPassword('');
    setAdditionalParams('');
  };

  // Get placeholders to use in inputs
  const placeholders = getFieldPlaceholders();

  // Check if this data source type supports connection string building
  if (![1, 2, 3].includes(dataSourceType)) {
    return null;
  }

  return (
    <Card className="p-4 border border-gray-200 rounded-md mt-2">
      <div className="text-sm font-medium mb-2">Connection String Builder</div>
      
      <div className="grid grid-cols-2 gap-3 mb-3">
        <div>
          <Label htmlFor="server" className="text-xs">Server</Label>
          <Input
            id="server"
            value={server}
            onChange={(e) => setServer(e.target.value)}
            placeholder={placeholders.server}
            className="h-8 text-sm"
          />
        </div>
        
        <div>
          <Label htmlFor="port" className="text-xs">Port</Label>
          <Input
            id="port"
            value={port}
            onChange={(e) => setPort(e.target.value)}
            placeholder={placeholders.port}
            className="h-8 text-sm"
          />
        </div>
      </div>
      
      <div className="mb-3">
        <Label htmlFor="database" className="text-xs">Database</Label>
        <Input
          id="database"
          value={database}
          onChange={(e) => setDatabase(e.target.value)}
          placeholder={placeholders.database}
          className="h-8 text-sm"
        />
      </div>
      
      <div className="grid grid-cols-2 gap-3 mb-3">
        <div>
          <Label htmlFor="username" className="text-xs">Username</Label>
          <Input
            id="username"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            placeholder={placeholders.username}
            className="h-8 text-sm"
          />
        </div>
        
        <div>
          <Label htmlFor="password" className="text-xs">Password</Label>
          <Input
            id="password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder={placeholders.password}
            className="h-8 text-sm"
          />
        </div>
      </div>
      
      <div className="mb-3">
        <Label htmlFor="additionalParams" className="text-xs">Additional Parameters</Label>
        <Input
          id="additionalParams"
          value={additionalParams}
          onChange={(e) => setAdditionalParams(e.target.value)}
          placeholder={placeholders.additionalParams}
          className="h-8 text-sm"
        />
      </div>
      
      <div className="flex space-x-2 justify-end">
        <Button 
          type="button" 
          variant="outline" 
          size="sm" 
          onClick={resetFields}
          className="text-xs"
        >
          Reset
        </Button>
        <Button 
          type="button" 
          size="sm" 
          onClick={applyConnectionString}
          className="text-xs"
        >
          Apply
        </Button>
      </div>
    </Card>
  );
};

export default ConnectionStringBuilder; 