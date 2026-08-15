import React from "react";
import { FaBolt, FaChartLine, FaBell, FaBuilding, FaLeaf, FaMobileAlt } from "react-icons/fa";

const featureList = [
  { icon: FaBolt, title: "Real-time Monitoring", desc: "Track energy consumption across all your organisations and devices in real-time with beautiful dashboards." },
  { icon: FaChartLine, title: "Advanced Analytics", desc: "Gain deep insights into your energy usage patterns with AI-powered analytics and predictive forecasting." },
  { icon: FaBell, title: "Smart Alerts", desc: "Receive instant notifications for unusual consumption patterns, budget overruns, or device malfunctions." },
  { icon: FaBuilding, title: "Multi-Organisation", desc: "Manage multiple organisations and locations from a single dashboard with role-based access control." },
  { icon: FaLeaf, title: "Carbon Footprint", desc: "Track and reduce your organisation's carbon emissions with detailed sustainability reporting." },
  { icon: FaMobileAlt, title: "Mobile Access", desc: "Monitor your energy consumption from anywhere with our fully responsive web and mobile applications." },
];

export default function Features() {
  return (
    <section id="features" className="space-y-12 pt-24">
      <div className="text-center space-y-4">
        <h2 className="text-3xl font-bold">Powerful Features for <span className="energy-gradient-text">Energy Intelligence</span></h2>
        <p className="text-gray-400 max-w-2xl mx-auto">Our platform offers everything you need to monitor, analyze, and optimize your organisation's energy consumption</p>
      </div>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {featureList.map((f, idx) => (
          <div key={idx} className="feature-card card-hover">
            <div className="w-14 h-14 energy-gradient rounded-lg flex items-center justify-center mb-4">
              <f.icon className="text-white text-2xl"/>
            </div>
            <h3 className="text-xl font-bold mb-2">{f.title}</h3>
            <p className="text-gray-300">{f.desc}</p>
          </div>
        ))}
      </div>
    </section>
  );
}
