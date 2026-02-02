# Outlook EmailService - Visitor Management Integration

## Description
This project is a **C# EmailService** that integrates **Microsoft Outlook calendar events** with a **Visitor Management Application**.

The service automatically processes Outlook calendar appointments and synchronizes relevant visit information (date, participants, location, etc.) with a backend system using **Supabase** and Hangfire for the Automation every 5 minutes.  
It runs on a scheduled interval and reacts to new or updated calendar entries.

## Purpose
The EmailService was developed to:
- Automate visitor registration workflows
- Reduce manual data entry
- Keep visitor data synchronized between Outlook and the Visitor App
- Ensure consistency when appointments are created or modified

## Tech Stack
- **C# (.NET)**
- **Microsoft Outlook / Exchange**
- **Supabase**
- **Swagger**
- **Hangfire**
- **SMTP / Email Processing**
- Multiple .NET libraries for:
  - Email handling
  - Scheduling
  - API communication
  - Data parsing

## How It Works

### 1️: Outlook Appointment Creation
Users create a calendar appointment in Outlook.

Relevant fields:
- **Title** → Used as appointment subject in the Visitor App
- **Required Attendees**
  - First internal email → Contact person
  - External emails → Visitors
- **Date & Time**
  - Start and end time of the visit
- **Location**
  - Must match predefined locations
- **Description**
  - Optional additional information

A service mailbox must be included as attendee: visitorapp@test-domain.com


### 2️: Automatic Processing
- The EmailService runs automated checks every **5 minutes**
- New or updated appointments are detected
- Data is extracted and validated
- Information is synchronized with Supabase
- Visitor records are created or updated accordingly

### 3️: Appointment Updates
If any of the following changes:
- Time
- Participants
- Location

The user simply updates the Outlook appointment.

The EmailService:
- Detects the change
- Updates the corresponding visitor data automatically

### 4️: Confirmation Email
Once an appointment is successfully processed:
- A confirmation email is sent to the user
- The email contains a direct link to the Visitor App
- The appointment details can be reviewed there

## Supported Locations (Example)
Only predefined locations are accepted to ensure data consistency, these can be always Changed in the Supabase Database:

- Main Office
- Office Room A
- Office Room B
- Open Space 1
- Open Space 2
- Meeting Room

*(These values can be configured and extended.)*

## Deleting Appointments
- Deleting an appointment in Outlook **does not automatically remove it** from the Visitor App
- Visitor entries must be removed manually within the application
- This behavior is intentional to prevent accidental data loss

## Service Flow
1. Outlook calendar event is created or updated
2. EmailService scans mailbox
3. Appointment data is parsed
4. Validation rules are applied
5. Data is stored or updated in Supabase
6. Confirmation email is sent

## Learning Outcomes
This project demonstrates experience with:

- C# service-based application development
- Outlook / Exchange automation
- Email parsing and processing
- Scheduled background jobs
- Supabase integration
- Data validation and synchronization
- Real-world business process automation
- Working with multiple external libraries

## Security & Privacy Notes
- No real email addresses or credentials are included
- Sensitive configuration values are excluded
- Test and placeholder data is used in this repository

## Starting and Using the EmailService

To run the EmailService, follow these steps:

1. **Prepare the MainApp**  
   Ensure the main Visitor Management Application (**BesucherApp**) is set up with **Supabase** and **Angular**.  

2. **Create Supabase Tables**  
   Create the specific tables required for both Supabase and the EmailService.  

3. **Configure RPC Functions**  
   Add the necessary Supabase RPC function calls so the EmailService can read/write data correctly.  

4. **Configure appsettings.json**  
   Add your API keys, host URL, private service email, and allowedHosts settings in `appsettings.json` for Outlook/SMTP communication.  

5. **Run the Project**  
   Open a terminal in the project folder and run:  
   ```bash
   dotnet run

6. **Access Documentation & Dashboard**

Navigate to http://localhost:<port>/swagger to see the API documentation
Navigate to http://localhost:<port>/hangfire to see the live Hangfire dashboard showing scheduled EmailService jobs
