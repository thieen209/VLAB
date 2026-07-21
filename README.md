<div align="center">

<img src="assets/logo.svg" width="160"/>

# VLAB

### Immersive STEM Education for Everyone

**Transforming smartphones into affordable Virtual Reality laboratories.**

[![Unity](https://img.shields.io/badge/Unity-6-black?logo=unity)]()
[![XR](https://img.shields.io/badge/XR-Toolkit-blue)]()
[![ESP32](https://img.shields.io/badge/ESP32-IoT-red)]()
[![BLE](https://img.shields.io/badge/Bluetooth-LE-0082FC?logo=bluetooth)]()
[![Android](https://img.shields.io/badge/Android-Supported-3DDC84?logo=android)]()
[![License](https://img.shields.io/badge/License-MIT-green)]()

</div>

---

# 🌍 Overview

**VLAB** is a low-cost Virtual Reality laboratory platform designed to make STEM practical education accessible to every student.

Instead of requiring expensive laboratory equipment or professional VR headsets, VLAB leverages devices students already own:

- 📱 Smartphone
- 🥽 Affordable VR Headset
- 🎮 ESP32 Motion Controllers

Together they create an immersive virtual laboratory where students can safely perform experiments in Physics, Chemistry, Biology, and Engineering.

---

# 🎯 Why VLAB?

Across many schools, especially in rural and underserved regions, laboratory education remains limited due to:

- Insufficient laboratory facilities
- High equipment costs
- Dangerous practical experiments
- Limited hands-on opportunities
- Unequal access to STEM education

VLAB bridges this gap by delivering realistic laboratory experiences through immersive Virtual Reality at a fraction of the traditional cost.

---

# ✨ Features

## 🧪 Physics Lab

- Projectile Motion
- Harmonic Oscillation
- Optics
- Magnetism
- Electric Circuits
- Mechanics

---

## ⚗️ Chemistry Lab

- Interactive Chemical Reactions
- Molecular Builder
- Safe Combustion Simulation
- Acid–Base Experiments
- Laboratory Equipment Interaction

---

## 🧬 Biology Lab

- Human Anatomy
- Cell Exploration
- Organ Systems
- Virtual Microscopy
- Immersive Biological Simulation

---

## ⚙️ Engineering Lab

- Circuit Design
- Robotics Simulation
- Mechanical Assembly
- Electronics Prototyping
- STEM Design Challenges

---

# 🚀 System Architecture

```text
          Student

             │
             ▼

     ESP32 Controllers
  (IMU + Buttons + Joystick)

             │
      ESP-NOW + BLE
             │

             ▼

      Smartphone App
       Unity XR Engine

             │

             ▼

       Virtual Laboratory

             │

             ▼

      Smartphone VR Headset

             │

             ▼

   Immersive STEM Experience
```

---

# 🛠 Hardware

| Component | Description |
|------------|-------------|
| ESP32 | Motion Controller |
| MPU9250 IMU | Motion Tracking |
| BLE | Wireless Communication |
| ESP-NOW | Controller Synchronization |
| Rechargeable Battery | Portable Power |
| Joystick | Navigation |
| Push Buttons | Interaction |
| 3D Printed Shell | Ergonomic Controller |
| Smartphone | Rendering Device |
| VR Headset | Immersive Display |

---

# 💻 Software Stack

- Unity 6
- Unity XR Interaction Toolkit
- C#
- Arduino Framework
- ESP-IDF
- Bluetooth Low Energy
- ESP-NOW
- Android

---

# 📂 Repository Structure

```text
VLAB
│
├── docs/
│
├── hardware/
│   ├── pcb/
│   ├── cad/
│   ├── enclosure/
│   └── schematics/
│
├── firmware/
│   ├── controller_left/
│   ├── controller_right/
│   └── shared/
│
├── unity/
│   ├── Assets/
│   ├── Packages/
│   ├── ProjectSettings/
│   └── Builds/
│
├── experiments/
│   ├── physics/
│   ├── chemistry/
│   ├── biology/
│   └── engineering/
│
├── assets/
│   ├── images/
│   ├── models/
│   ├── textures/
│   └── audio/
│
├── research/
│
├── LICENSE
└── README.md
```

---

# 🎮 Workflow

```text
Hand Movement
      │
      ▼
ESP32 Controller
      │
      ▼
Motion Sensor (IMU)
      │
      ▼
Bluetooth LE
      │
      ▼
Unity XR
      │
      ▼
Virtual Hands
      │
      ▼
Laboratory Interaction
```

---

# 🎓 Educational Impact

VLAB enables students to

- Learn through immersive interaction
- Practice unlimited times
- Explore dangerous experiments safely
- Understand complex scientific concepts visually
- Develop engineering thinking through hands-on virtual experiences

---

# 💰 Accessibility

Traditional VR laboratories often cost thousands of dollars.

VLAB reduces the required hardware to approximately

| Item | Estimated Cost |
|-------|---------------:|
| ESP32 Controller | ~$4 |
| IMU Sensor | ~$3 |
| VR Headset | ~$4 |
| Battery | ~$2 |
| Buttons & Joystick | ~$1 |
| 3D Printed Housing | ~$2 |

**Estimated total: under $20 per setup (excluding smartphone).**

---

# 🗺 Roadmap

- [x] ESP32 Motion Controller
- [x] BLE Communication
- [x] Unity XR Integration
- [x] Physics Laboratory
- [ ] Chemistry Laboratory
- [ ] Biology Laboratory
- [ ] Engineering Laboratory
- [ ] Teacher Dashboard
- [ ] Cloud Experiment Library
- [ ] AI Learning Assistant
- [ ] Classroom Multiplayer
- [ ] Learning Analytics

---

# 🤝 Contributing

Contributions are welcome.

Whether you're interested in:

- VR Development
- Unity
- Embedded Systems
- ESP32
- 3D Design
- STEM Education
- UX/UI
- Educational Content

feel free to open an Issue or submit a Pull Request.

---

# 📖 Citation

If you use VLAB in research or educational projects, please cite this repository.

---

# 🌟 Vision

> **Every student deserves access to a laboratory.**
>
> VLAB aims to democratize STEM education by making immersive virtual laboratories affordable, scalable, and available to schools everywhere.

---

<div align="center">

### Built with ❤️ for STEM Education

**Making immersive learning accessible to every classroom.**

</div>
