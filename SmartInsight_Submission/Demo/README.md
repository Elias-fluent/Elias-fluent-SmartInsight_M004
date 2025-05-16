# SmartInsight

SmartInsight is an enterprise knowledge exploration platform that leverages AI to provide natural language access to organizational data.

## Demo Video

**Note: Due to time constraints, the demo video could not be completed for this submission.**

The planned video would have showcased:

1. User authentication and tenant selection
2. Natural language querying
3. Visualization of results
4. Knowledge graph exploration
5. Data source integration
6. Administrative features

For details on these features, please refer to the Technical Overview and Architecture documents.

## Installation Requirements

### Prerequisites
- Docker Desktop (20.10.0 or higher)
- 16GB RAM minimum (32GB recommended)
- 50GB free disk space
- Internet connection (for initial setup)

### Getting Started

1. **Clone the repository**
   ```bash
   git clone https://github.com/Elias-fluent/Elias-fluent-SmartInsight_M004
   cd smartinsight
   ```

2. **Configure environment variables**
   ```bash
   cp .env.example .env
   ```
   Edit the `.env` file with appropriate values for your environment.

3. **Start the application**
   ```bash
   docker-compose up -d
   ```
   The initial startup will download necessary images and may take several minutes.

4. **Access the application**
   
   Open your browser and navigate to:
   - User Interface: `http://localhost:3000`
   - Admin Interface: `http://localhost:3001`
   - API Documentation: `http://localhost:5000/swagger`

## Default Credentials

The system comes with default accounts for demonstration purposes:

### Admin User
- Email: `admin@smartinsight.demo`
- Password: `SmartInsight!Demo2025`

### Test Users
- Email: `analyst@smartinsight.demo` 
- Password: `SmartInsight!Demo2025`
  
- Email: `viewer@smartinsight.demo`
- Password: `SmartInsight!Demo2025`

## Demo Data Sources

The demonstration environment includes pre-configured data sources:

1. **Sample PostgreSQL Database**
   - Contains organizations, employees, projects, and financial data
   - Automatically populated with sample data on startup

2. **Document Repository**
   - Contains sample documents (.txt, .md, .pdf, .docx)
   - Located in `/sample-data/documents`

## Local Development

For development purposes, you can run individual components:

1. **Backend Services**
   ```bash
   cd src
   dotnet run --project SmartInsight.API
   ```

2. **Frontend Development**
   ```bash
   cd src/SmartInsight.UI
   npm install
   npm run dev
   ```

## Troubleshooting

### Common Issues

1. **Docker Container Fails to Start**
   - Ensure Docker Desktop is running
   - Check for port conflicts (3000, 3001, 5000, 5432, 6333)
   - Verify memory allocation in Docker Desktop settings

2. **Database Connection Issues**
   - Check PostgreSQL container logs: `docker-compose logs postgres`
   - Verify connection string in `.env` file

3. **UI Cannot Connect to API**
   - Check API container logs: `docker-compose logs api`
   - Verify API URL in UI configuration

## Additional Resources

- Full documentation is available in the `/docs` directory
- API reference is available at `http://localhost:5000/swagger`
- Source code is organized as described in the Technical Overview document

## Support

For support inquiries, please contact:
- Email: elias.dergham@fluenttechnology.com
- GitHub Issues: https://github.com/Elias-fluent/Elias-fluent-SmartInsight_M004

## License

This project is licensed under the terms of the MIT license. See `LICENSE.txt` for more details. 