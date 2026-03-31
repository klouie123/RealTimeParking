# 🚗 Real-Time Parking System

## 📌 Overview

The **Real-Time Parking System** is a mobile application developed using **.NET MAUI** with an **ASP.NET Core Web API backend**.
It helps users find the nearest available parking locations and navigate to them in real time.

This system improves parking efficiency by allowing users to:

* Search for nearby parking spaces
* Reserve parking slots
* Navigate using map-based GPS routing
* Manage active reservations

---

## 🎯 Features

### 🔐 Authentication System

* User registration and login
* Persistent login (auto-login using local storage)

### 📍 Parking Search

* Quick search based on user's current location
* Displays available parking locations from database

### 🅿️ Parking Slot Reservation

* View parking slots per location
* Reserve available slots
* Reservation expires automatically (1-hour limit)

### 🧭 Navigation System

* Real-time navigation using map
* Displays:

  * Distance
  * Estimated Time of Arrival (ETA)
  * Speed
* Route updates dynamically

### 📊 Reservation Management

* View active reservation (bottom panel UI)
* Navigate to reserved parking
* Cancel reservation (slot becomes available again)

---

## 🏗️ System Architecture

### 📱 Frontend

* **.NET MAUI**
* MVVM-style structure
* Uses device features:

  * GPS (Geolocation)
  * Local storage (Preferences)

### 🌐 Backend

* **ASP.NET Core Web API**
* Entity Framework Core
* SQL Server Database

### 🗄️ Database Tables

* Users
* ParkingLocations
* ParkingSlots
* ParkingReservations

---

## ⚙️ Technologies Used

* .NET MAUI
* ASP.NET Core Web API
* C#
* SQL Server
* Entity Framework Core
* Maps / Geolocation APIs

---

## 🚀 How It Works

1. User logs in or signs up
2. System detects user location
3. Displays nearby parking locations
4. User selects a location and views slots
5. User reserves a slot
6. User navigates to the parking location
7. Reservation expires automatically if unused

---

## 📦 Installation

### 🔧 Requirements

* Visual Studio 2022/2026
* .NET SDK
* Android Emulator or Physical Device
* SQL Server

### ▶️ Steps

1. Clone the repository:

```bash
git clone https://github.com/YOUR_USERNAME/YOUR_REPO.git
```

2. Open the solution in Visual Studio

3. Setup database connection in:

```
appsettings.json
```

4. Run the API first

5. Run the MAUI app

---

## 🔐 Notes

* Reservation expires after **1 hour**
* Navigation uses device GPS
* Internet connection is required

---

## 👨‍💻 Developers

* Klouie123

---

## 📄 License

This project is for **academic purposes only**.
Unauthorized use or distribution is not allowed.

---
