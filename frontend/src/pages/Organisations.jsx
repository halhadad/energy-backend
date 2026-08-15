import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import OrganisationForm from "../components/OrganisationForm";
import api from "../api/axios";
import { useDispatch, useSelector } from "react-redux";
import { setOrganisations as setUserOrganisations } from "../redux/user/userSlice";
import { FaBuilding, FaPlug, FaWallet, FaPlus } from "react-icons/fa";

const OrganisationCard = ({
  organisationId,
  name,
  type,
  monthlyBudgetUsd,
  deviceCount,
  onEdit,
  onDelete,
}) => {
  const navigate = useNavigate();

  const handleManageOrg = () => {
    navigate(`/organisations/devices/${organisationId}`);
  };

  return (
    <div className="stat-card flex flex-col justify-between min-h-[210px]">
      <div>
        <div className="flex justify-between items-start mb-3 gap-3">
          <div>
            <h2 style={{ fontSize: "1rem", fontWeight: 600, color: "var(--text-primary)" }}>{name}</h2>
            <p style={{ fontSize: "0.75rem", color: "var(--text-muted)", marginTop: 2 }}>
              <FaBuilding style={{ display: "inline", marginRight: 6 }} />
              {type || "Unspecified"}
            </p>
          </div>
          <div className="flex gap-2 text-sm">
            <button
              className="btn-accent"
              onClick={() =>
                onEdit({ organisationId, name, type, monthlyBudgetUsd })
              }
            >
              Edit
            </button>
            <button
              className="btn-danger"
              onClick={() => onDelete(organisationId)}
            >
              Delete
            </button>
          </div>
        </div>
        <div className="space-y-2">
          <p style={{ fontSize: "0.8125rem", color: "var(--text-secondary)" }}>
            <FaPlug style={{ display: "inline", marginRight: 6 }} />
            Devices: <span style={{ color: "var(--text-primary)", fontWeight: 600 }}>{deviceCount} connected</span>
          </p>
          <p style={{ fontSize: "0.8125rem", color: "var(--text-secondary)" }}>
            <FaWallet style={{ display: "inline", marginRight: 6 }} />
            Monthly Budget:{" "}
            <span style={{ color: "var(--green)", fontWeight: 600 }}>
              {(monthlyBudgetUsd ?? 0) > 0 ? `$${Number(monthlyBudgetUsd).toFixed(2)}` : "Not set"}
            </span>
          </p>
        </div>
      </div>

      <div className="flex justify-end mt-5">
        <button
          onClick={handleManageOrg}
          className="btn-primary"
          style={{ padding: "0.45rem 0.9rem", fontSize: "0.8125rem" }}
        >
          Manage Devices
        </button>
      </div>
    </div>
  );
};

const AddOrganisationCard = ({ onClick }) => (
  <button
    type="button"
    onClick={onClick}
    className="stat-card flex flex-col items-center justify-center gap-3 cursor-pointer min-h-[210px] w-full"
    style={{ borderStyle: "dashed", background: "var(--bg-surface)", color: "var(--text-secondary)" }}
  >
    <span
      className="flex items-center justify-center"
      style={{
        width: 44,
        height: 44,
        borderRadius: "50%",
        border: "1px solid var(--border)",
      }}
    >
      <FaPlus style={{ fontSize: "1rem" }} />
    </span>
    <span style={{ fontSize: "0.875rem", fontWeight: 500 }}>Add Organisation</span>
  </button>
);

export default function Organisations() {
  const [organisations, setOrganisations] = useState([]);
  const [showModal, setShowModal] = useState(false);
  const [editingOrg, setEditingOrg] = useState(null);
  const dispatch = useDispatch();

  const user = useSelector((state) => state.user);
  const token = user?.accessToken;


  useEffect(() => {
    const loadOrgs = async () => {
      try {
        const res = await api.get("/Organisation");
        setOrganisations(res.data);
        dispatch(setUserOrganisations(res.data));
      } catch (err) {
        console.error("Failed to load organisations", err);
      }
    };

    loadOrgs();
  }, [token]);


  const handleAddOrEditOrg = async (orgData) => {
    try {
      if (editingOrg) {
        const res = await api.put(
          `/Organisation/${editingOrg.organisationId}`,
          orgData
        );
        const updated = organisations.map((org) =>
          org.organisationId === editingOrg.organisationId ? res.data : org
        );
        setOrganisations(updated);
        dispatch(setUserOrganisations(updated));
      } else {
        const res = await api.post("/Organisation", orgData);
        const updated = [...organisations, res.data];
        setOrganisations(updated);
        dispatch(setUserOrganisations(updated));
      }
      setEditingOrg(null);
      setShowModal(false);
    } catch (err) {
      console.error("Failed to save organisation", err);
      alert("Failed to save organisation.");
    }
  };

  const handleEdit = (org) => {
    setEditingOrg(org);
    setShowModal(true);
  };

  const handleDelete = async (id) => {
    const confirmDelete = window.confirm(
      "Are you sure you want to delete this organisation?"
    );
    if (!confirmDelete) return;

    try {
      await api.delete(`/Organisation/${id}`);
      const updated = organisations.filter((org) => org.organisationId !== id);
      setOrganisations(updated);
      dispatch(setUserOrganisations(updated));
    } catch (err) {
      console.error("Failed to delete organisation", err);
      alert("Failed to delete organisation.");
    }
  };

  return (
    <div className="p-4 fade-in">
      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3">
        {organisations.map((org) => (
          <OrganisationCard
            key={org.organisationId}
            {...org}
            onEdit={handleEdit}
            onDelete={handleDelete}
          />
        ))}
        <AddOrganisationCard
          onClick={() => {
            setEditingOrg(null);
            setShowModal(true);
          }}
        />
      </div>

      <OrganisationForm
        isOpen={showModal}
        onClose={() => setShowModal(false)}
        onSubmit={handleAddOrEditOrg}
        initialData={editingOrg}
      />
    </div>
  );
}
