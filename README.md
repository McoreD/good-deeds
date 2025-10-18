# Good Deeds Tracker

## Overview
Good Deeds Tracker is a lightweight cloud application that helps parents record their children’s good and bad deeds, assign points, and automatically calculate rewards in dollars.  
It runs entirely on free or near-free cloud services and uses serverless technology to keep costs close to zero.

## Architecture
| Layer | Technology | Purpose |
|-------|-------------|----------|
| Front-end | **Blazor WebAssembly** | Browser-based client built in C# |
| Back-end | **Azure Functions (.NET isolated)** | Serverless API for deeds, balances, and redemptions |
| Database | **Neon Postgres (Free tier)** | Persistent data storage |
| Hosting | **Azure Static Web Apps (Free)** | Hosts the Blazor app and Functions API |
| Development | **GitHub Codespaces** | Cloud IDE for coding and testing |

## Features
- Parent registration and login  
- Create and manage child profiles  
- Define deed types (positive or negative points)  
- Log deeds with timestamps and notes  
- Compute current point and dollar balances  
- Redeem points for rewards  
- Dashboard to view children, deeds, and balances  
- Export data to CSV or PDF  
- Zero-idle-cost cloud design  

## Folder Structure
