# SmartInsight Sequence Diagrams

This document illustrates the key system workflows using sequence diagrams. These diagrams show the interactions between components and the flow of data throughout the system.

## 1. User Authentication Flow

```
┌─────┐          ┌─────┐          ┌──────────────┐          ┌─────────────┐
│ User│          │ UI  │          │SmartInsight.API│          │Identity Provider│
└──┬──┘          └──┬──┘          └───────┬──────┘          └──────┬──────┘
   │    Login       │                     │                        │
   │ ───────────────>                     │                        │
   │                │                     │                        │
   │                │   Authentication    │                        │
   │                │  ──────────────────>                         │
   │                │                     │                        │
   │                │                     │  Authentication Request│
   │                │                     │ ─────────────────────>│
   │                │                     │                        │
   │                │                     │                        │
   │                │                     │    Auth Response       │
   │                │                     │ <─────────────────────│
   │                │                     │                        │
   │                │  Auth Token         │                        │
   │                │ <──────────────────│                        │
   │                │                     │                        │
   │   Auth Success │                     │                        │
   │ <───────────────                     │                        │
┌──┴──┐          ┌──┴──┐          ┌───────┴──────┐          ┌──────┴──────┐
│ User│          │ UI  │          │SmartInsight.API│          │Identity Provider│
└─────┘          └─────┘          └──────────────┘          └─────────────┘
```

### Authentication Flow Description

1. User enters credentials in the UI
2. UI sends authentication request to SmartInsight.API
3. SmartInsight.API forwards the request to the Identity Provider
4. Identity Provider validates credentials and issues a token
5. SmartInsight.API receives the token and returns it to UI
6. UI stores the token for subsequent requests and shows the authenticated view

## 2. Natural Language Query Processing

```
┌─────┐       ┌─────┐       ┌──────────────┐       ┌───────────────┐       ┌─────────────────┐       ┌────────┐
│ User│       │ UI  │       │SmartInsight.API│       │SmartInsight.AI│       │SmartInsight.Knowledge│       │Database│
└──┬──┘       └──┬──┘       └───────┬──────┘       └──────┬────────┘       └──────────┬──────┘       └───┬────┘
   │  Query      │                  │                     │                           │                   │
   │ ────────────>                  │                     │                           │                   │
   │              │                 │                     │                           │                   │
   │              │ Query Request   │                     │                           │                   │
   │              │ ────────────────>                     │                           │                   │
   │              │                 │                     │                           │                   │
   │              │                 │ Process Query       │                           │                   │
   │              │                 │ ────────────────────>                           │                   │
   │              │                 │                     │                           │                   │
   │              │                 │                     │ Generate Embedding        │                   │
   │              │                 │                     │ ──────────────────────────>                   │
   │              │                 │                     │                           │                   │
   │              │                 │                     │                           │ Vector Search     │
   │              │                 │                     │                           │ ────────────────>│
   │              │                 │                     │                           │                   │
   │              │                 │                     │                           │ Search Results    │
   │              │                 │                     │                           │ <────────────────│
   │              │                 │                     │                           │                   │
   │              │                 │                     │ Matches                   │                   │
   │              │                 │                     │ <──────────────────────────                   │
   │              │                 │                     │                           │                   │
   │              │                 │                     │ Fetch Data                │                   │
   │              │                 │                     │ ────────────────────────────────────────────>│
   │              │                 │                     │                           │                   │
   │              │                 │                     │ Data Results              │                   │
   │              │                 │                     │ <────────────────────────────────────────────│
   │              │                 │                     │                           │                   │
   │              │                 │ Analysis Results    │                           │                   │
   │              │                 │ <────────────────────                           │                   │
   │              │                 │                     │                           │                   │
   │              │ Results         │                     │                           │                   │
   │              │ <────────────────                     │                           │                   │
   │              │                 │                     │                           │                   │
   │ Results      │                 │                     │                           │                   │
   │ <────────────│                 │                     │                           │                   │
┌──┴──┐       ┌──┴──┐       ┌───────┴──────┐       ┌──────┴────────┐       ┌──────────┴──────┐       ┌───┴────┐
│ User│       │ UI  │       │SmartInsight.API│       │SmartInsight.AI│       │SmartInsight.Knowledge│       │Database│
└─────┘       └─────┘       └──────────────┘       └───────────────┘       └─────────────────┘       └────────┘
```

### Query Processing Flow Description

1. User enters a natural language query in the UI
2. UI sends the query to SmartInsight.API
3. API forwards the query to SmartInsight.AI for processing
4. AI generates a vector embedding for the query
5. AI requests vector search from Knowledge component
6. Knowledge executes similarity search in Qdrant
7. Knowledge returns matching results to AI
8. AI requests additional data from the database
9. AI processes and analyzes the data
10. AI returns structured results to the API
11. API returns formatted results to the UI
12. UI renders the results to the user

## 3. Data Import and Processing

```
┌─────┐       ┌─────┐       ┌──────────────┐       ┌──────────────┐       ┌───────────────┐       ┌─────────────────┐
│ User│       │ UI  │       │SmartInsight.API│       │SmartInsight.Data│       │SmartInsight.AI│       │SmartInsight.Knowledge│
└──┬──┘       └──┬──┘       └───────┬──────┘       └──────┬───────┘       └──────┬────────┘       └──────────┬──────┘
   │ Upload Data  │                 │                     │                      │                           │
   │ ────────────>│                 │                     │                      │                           │
   │              │                 │                     │                      │                           │
   │              │ Import Request  │                     │                      │                           │
   │              │ ────────────────>                     │                      │                           │
   │              │                 │                     │                      │                           │
   │              │                 │ Store Raw Data      │                      │                           │
   │              │                 │ ────────────────────>                      │                           │
   │              │                 │                     │                      │                           │
   │              │                 │ Data Stored         │                      │                           │
   │              │                 │ <────────────────────                      │                           │
   │              │                 │                     │                      │                           │
   │              │                 │ Process Data        │                      │                           │
   │              │                 │ ─────────────────────────────────────────>│                           │
   │              │                 │                     │                      │                           │
   │              │                 │                     │                      │ Generate Embeddings       │
   │              │                 │                     │                      │ ──────────────────────────>
   │              │                 │                     │                      │                           │
   │              │                 │                     │                      │ Embeddings Stored         │
   │              │                 │                     │                      │ <──────────────────────────
   │              │                 │                     │                      │                           │
   │              │                 │ Generate Insights   │                      │                           │
   │              │                 │ <─────────────────────────────────────────│                           │
   │              │                 │                     │                      │                           │
   │              │ Import Complete │                     │                      │                           │
   │              │ <────────────────                     │                      │                           │
   │              │                 │                     │                      │                           │
   │ Notification │                 │                     │                      │                           │
   │ <────────────│                 │                     │                      │                           │
┌──┴──┐       ┌──┴──┐       ┌───────┴──────┐       ┌──────┴───────┐       ┌──────┴────────┐       ┌──────────┴──────┐
│ User│       │ UI  │       │SmartInsight.API│       │SmartInsight.Data│       │SmartInsight.AI│       │SmartInsight.Knowledge│
└─────┘       └─────┘       └──────────────┘       └──────────────┘       └───────────────┘       └─────────────────┘
```

### Data Import Flow Description

1. User uploads data through the UI
2. UI sends the data import request to SmartInsight.API
3. API forwards the data to SmartInsight.Data for storage
4. Data component stores the raw data in the database
5. API requests SmartInsight.AI to process the data
6. AI analyzes the data and generates insights
7. AI requests Knowledge component to store vector embeddings
8. AI returns processing results to API
9. API notifies the UI that processing is complete
10. UI shows a notification to the user

## 4. Dashboard Generation

```
┌─────┐       ┌─────┐       ┌──────────────┐       ┌───────────────┐       ┌────────────────┐
│ User│       │ UI  │       │SmartInsight.API│       │SmartInsight.Data│       │SmartInsight.History│
└──┬──┘       └──┬──┘       └───────┬──────┘       └──────┬────────┘       └──────┬─────────┘
   │ Request     │                  │                     │                       │
   │ Dashboard   │                  │                     │                       │
   │ ────────────>                  │                     │                       │
   │              │                 │                     │                       │
   │              │ Dashboard       │                     │                       │
   │              │ Request         │                     │                       │
   │              │ ────────────────>                     │                       │
   │              │                 │                     │                       │
   │              │                 │ Fetch Data          │                       │
   │              │                 │ ────────────────────>                       │
   │              │                 │                     │                       │
   │              │                 │ Current Data        │                       │
   │              │                 │ <────────────────────                       │
   │              │                 │                     │                       │
   │              │                 │ Fetch Historical    │                       │
   │              │                 │ Data                │                       │
   │              │                 │ ────────────────────────────────────────>  │
   │              │                 │                     │                       │
   │              │                 │ Historical Data     │                       │
   │              │                 │ <────────────────────────────────────────  │
   │              │                 │                     │                       │
   │              │                 │ Generate            │                       │
   │              │                 │ Dashboard           │                       │
   │              │                 │ Components          │                       │
   │              │                 │                     │                       │
   │              │ Dashboard       │                     │                       │
   │              │ Data            │                     │                       │
   │              │ <────────────────                     │                       │
   │              │                 │                     │                       │
   │ Render       │                 │                     │                       │
   │ Dashboard    │                 │                     │                       │
   │ <────────────│                 │                     │                       │
┌──┴──┐       ┌──┴──┐       ┌───────┴──────┐       ┌──────┴────────┐       ┌──────┴─────────┐
│ User│       │ UI  │       │SmartInsight.API│       │SmartInsight.Data│       │SmartInsight.History│
└─────┘       └─────┘       └──────────────┘       └───────────────┘       └────────────────┘
```

### Dashboard Generation Flow Description

1. User requests a dashboard view
2. UI sends the dashboard request to SmartInsight.API
3. API requests current data from SmartInsight.Data
4. API requests historical data from SmartInsight.History
5. API processes the data and generates dashboard components
6. API returns the dashboard data to the UI
7. UI renders the dashboard for the user

## 5. User Management Flow

```
┌──────┐       ┌─────┐       ┌──────────────┐       ┌─────────────────┐       ┌─────────────┐
│ Admin│       │ UI  │       │SmartInsight.API│       │SmartInsight.Admin│       │Identity Provider│
└──┬───┘       └──┬──┘       └───────┬──────┘       └────────┬────────┘       └──────┬──────┘
   │ Create User  │                  │                       │                       │
   │ ────────────>                   │                       │                       │
   │               │                 │                       │                       │
   │               │ User Creation   │                       │                       │
   │               │ Request         │                       │                       │
   │               │ ────────────────>                       │                       │
   │               │                 │                       │                       │
   │               │                 │ Create User           │                       │
   │               │                 │ ───────────────────────>                      │
   │               │                 │                       │                       │
   │               │                 │                       │ Create Identity       │
   │               │                 │                       │ ──────────────────────>
   │               │                 │                       │                       │
   │               │                 │                       │ Identity Created      │
   │               │                 │                       │ <──────────────────────
   │               │                 │                       │                       │
   │               │                 │ User Created          │                       │
   │               │                 │ <───────────────────────                      │
   │               │                 │                       │                       │
   │               │ Creation        │                       │                       │
   │               │ Success         │                       │                       │
   │               │ <────────────────                       │                       │
   │               │                 │                       │                       │
   │ Confirmation  │                 │                       │                       │
   │ <────────────│                  │                       │                       │
┌──┴───┐       ┌──┴──┐       ┌───────┴──────┐       ┌────────┴────────┐       ┌──────┴──────┐
│ Admin│       │ UI  │       │SmartInsight.API│       │SmartInsight.Admin│       │Identity Provider│
└──────┘       └─────┘       └──────────────┘       └─────────────────┘       └─────────────┘
```

### User Management Flow Description

1. Admin submits a user creation request
2. UI sends the request to SmartInsight.API
3. API forwards the request to SmartInsight.Admin
4. Admin component creates the user in the system database
5. Admin component requests identity creation from the Identity Provider
6. Identity Provider creates the user identity
7. Admin component completes the user creation process
8. API returns success status to the UI
9. UI shows confirmation to the admin

## Summary

These sequence diagrams illustrate the key workflows in the SmartInsight system, showing how components interact to fulfill user requests. They demonstrate the separation of concerns in the architecture, with each component responsible for specific aspects of the system functionality. 