import React from "react";
import { FaRocket, FaCheckCircle } from "react-icons/fa";
import { Link } from "react-router-dom";

export default function Hero() {
  return (
    <section className="grid grid-cols-1 lg:grid-cols-2 gap-12 items-center">
      <div className="space-y-8">
        <div className="space-y-4">
          <h2 className="text-4xl md:text-5xl font-bold leading-tight">
            Intelligent Energy Monitoring for{" "}
            <span className="energy-gradient-text">Modern Businesses</span>
          </h2>
          <p className="text-xl text-gray-300">
            Monitor, analyze, and optimize energy consumption across your
            organisation with our AI-powered platform. Reduce costs and carbon
            footprint with real-time insights.
          </p>
        </div>
        <div className="flex flex-col sm:flex-row gap-4">
          <Link to="/signin" className="btn-primary text-lg px-8 py-4 flex items-center justify-center gap-2">
            <FaRocket /> Get Started Now
          </Link>

        </div>
        <div className="flex items-center space-x-6 pt-4">
          <FeatureCheck label="Real-time Monitoring" />
          <FeatureCheck label="Cost Reduction" />
        </div>
      </div>
      <div className="relative">{/* Add your panel here */}</div>
    </section>
  );
}

function FeatureCheck({ label }) {
  return (
    <div className="flex items-center">
      <FaCheckCircle className="text-cyan-400 mr-2" />
      <span className="text-gray-300">{label}</span>
    </div>
  );
}
