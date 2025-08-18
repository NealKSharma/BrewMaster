# Online Retail Management System

A full-stack e-commerce web application designed to provide a scalable, secure, and user-friendly platform for managing online retail operations. Demonstrated as **BrewMaster**, a simulated coffee retail store, this domain-independent solution can be adapted to any retail sector.

---

## Table of Contents

1. [Features](#features)  
2. [Interface](#interface)  
3. [Architecture & Implementation](#architecture--implementation)  
4. [Technology Stack](#technology-stack)  
5. [Development Highlights](#development-highlights)  
6. [Database Design](#database-design)  
7. [Setup & Installation](#setup--installation)  
8. [Author](#author)  
9. [License](#license)  

---

## Features

### User Authentication & Account Management

- Secure registration and login with SHA256 hashing + salt  
- Unique username and email validation  
- Multi-step forgot password flow using security questions and AJAX form switching  
- Role-based access control (Admin vs User) via a custom authorization attribute  
- Selective profile updates without overwriting unchanged fields  

### Admin Features

- **Dashboard:** Quick overview of orders, products, users, and activity logs  
- **Product management:** add, edit, and delete products with image uploads stored as binary data. Live product table with clickable rows to autofill the edit form
- AJAX-driven operations for seamless UI experience  
- **User management:** searchable and sortable user table, selective field updates  
- **Logs management:** audit trails for errors, logins, and product and user updates; searchable and filterable  

### Dynamic Product Listing

The platform features a fully dynamic product listing page that pulls available products from the database in real-time. Products can be added, removed or updated using the above-mentioned product management page. 
- Real-time data binding from SQL Server  
- Placeholder image fallback when product image is missing  
- Stock status indicators; out-of-stock items hidden automatically  
- AJAX-powered Add to Cart button with interactive feedback and toast notifications  

### Shopping Cart & Checkout

- View and manage cart items with images, names, prices, stock levels, and totals  
- Adjust quantities inline; zero quantity removes item automatically  
- Dynamic order summary sidebar with item count, subtotal, and total  
- AJAX-based updates and toast notifications for cart actions  
- Secure checkout flow that moves cart data into orders table  

---
## Interface

### Landing Page 
<img width="455" alt="image" src="https://github.com/user-attachments/assets/2088ee2c-de2c-4ece-85e0-fadaba8dd5b9" />
<img width="455" alt="image" src="https://github.com/user-attachments/assets/cc0b8c9e-1ac4-483e-8710-51dfcf1b322a" />

### Sign Up and Login Pages
<img width="455" alt="image" src="https://github.com/user-attachments/assets/6715dc27-1cda-4b8b-bf4c-7ca22d8571d0" />
<img width="455" alt="image" src="https://github.com/user-attachments/assets/b608ec1a-241d-4298-9304-b92343ed6808" />
<img width="455" alt="image" src="https://github.com/user-attachments/assets/fce1ddcb-0c0a-426b-aa30-21ce1286a2c5" />
<img width="455" alt="image" src="https://github.com/user-attachments/assets/e6fea930-d7f1-4bf7-b494-be6fffe48125" />

### Admin - Dashboard and Log Management Pages
<img width="455" alt="image" src="https://github.com/user-attachments/assets/018474bd-10fd-44e7-95ac-e58c20018095" />
<img width="455" alt="image" src="https://github.com/user-attachments/assets/8f95b07a-6aaf-4113-aed5-9d0fc07b43a6" />

### Admin - User and Product Management Pages
<img width="455" alt="image" src="https://github.com/user-attachments/assets/abdb5298-1bf5-432b-964b-d6c1aa5b368a" />
<img width="455" alt="image" src="https://github.com/user-attachments/assets/f67f80cc-acf0-4457-b723-c601bc61744b" />

### User - Dynamic Product Listing and Cart Pages
<img width="455" alt="image" src="https://github.com/user-attachments/assets/d464977f-fcfe-4638-bfdf-8a8cc9ac17d0" />
<img width="455" alt="image" src="https://github.com/user-attachments/assets/9e07d07a-7f8a-41e6-9701-b39f7da973ea" />

### User - Account Page
<img width="455" alt="image" src="https://github.com/user-attachments/assets/2d7c6499-602c-464d-b915-ddca94168450" />

---

## Architecture & Implementation

This project follows the MVC (Model-View-Controller) design pattern for clear separation of concerns:

- **Models** encapsulate data entities, validation rules, and business logic  
- **Views** use Razor pages to render the UI dynamically  
- **Controllers** handle requests, orchestrate data flow, and call the centralized Database Helper for all ADO.NET operations  
- **Database Helper** abstracts and manages connections, commands, and parameter binding  

---

## Technology Stack

| Layer        | Technologies                                   |
| ------------ | ---------------------------------------------- |
| Backend      | C#, ASP.NET Core MVC, ADO.NET, SQL Server      |
| Frontend     | HTML, CSS (custom theme), JavaScript, jQuery |
| Security     | SHA256 + salt hashing, parameterized queries, client/server validation, role-based auth  |
| Tooling      | Visual Studio, SQL Server Management Studio    |

---

## Development Highlights

- Migrated from ASP.NET Web Forms to ASP.NET Core MVC for modern architecture and as a learning experience 
- Extensive AJAX interactions for admin operations and cart management  
- Responsive UI with consistent dark brown and gold theme and flexible grid layouts  
- Robust image handling: stored as byte arrays, crash issues resolved in ADO.NET binding  
- Centralized logging for transparency and real-time debugging  

---

## Database Design

The database schema includes:

- **Users** table for account details and securely hashed passwords  
- **Products** table with product metadata and binary image storage  
- **Orders** table to manage checkout data  
- **Logs** tables capturing logins, errors, user updates, and product updates  
- Indexes, primary and foreign keys, ensure performance and integrity.

**A full setup script with sample data is provided.**

---

## Setup

1. Prerequisites: SQL Server and Visual Studio (ASP.NET workload)

2. To set up the database, run DatabaseSetup.sql in SSMS. This creates all the tables, stored procedures, and triggers for the database titled "BrewMaster"

3. In appsettings.json, update:
   "DefaultConnection": "Server=YOUR_SERVER;Database=BrewMaster;User Id=sa;Password=YourPass;"

4. Open BrewMaster.sln in Visual Studio and run the application

5. Upon the first launch, you will have to create a user and an admin account for testing. Click on SignUp and create two accounts with different usernames. Initially, both accounts will have user access.

6. Grant Admin in SSMS:
   UPDATE tblUserMaster set UserRole = 'Admin' Where UserId = 1
   OR
   UPDATE tblUserMaster set UserRole = 'Admin' Where UserName = 'username_you_chose'

7. The website will now be set up with dummy products using the same placeholder images.
If you wish to change these placeholder images, log in with the admin account and go to the Product Management page. Then, click on a product, add an image, and update it.

---

## Author

Neal Kaushik Sharma <br>
Sophomore, Computer Science <br>
Iowa State University

Feel free to get in touch with me on my [LinkedIn](https://www.linkedin.com/in/nealksharma/).

---

## License

Copyright © 2025-Present, Neal Kaushik Sharma

