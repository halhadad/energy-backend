import { useState, useEffect, useCallback } from "react";
import Loading from "../components/Loading";
import { useParams, useNavigate, useSearchParams } from "react-router-dom";
import DeviceForm from "../components/DeviceForm";
import api from "../api/axios";

const PAGE_SIZE = 10;

export default function Devices() {
  const { orgId } = useParams();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();

  const currentPage = Math.max(1, parseInt(searchParams.get("page") ?? "1", 10));

  const [items, setItems] = useState([]);
  const [total, setTotal] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [orgName, setOrgName] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingDevice, setEditingDevice] = useState(null);

  const goToPage = useCallback(
    (p) => setSearchParams({ page: String(p) }),
    [setSearchParams]
  );

  const loadPage = useCallback(
    async (page) => {
      setLoading(true);
      setError(null);
      try {
        const [devRes, orgRes] = await Promise.all([
          api.get(`/Device/byOrganisation/${orgId}/paged?page=${page}&pageSize=${PAGE_SIZE}`),
          api.get("/Organisation"),
        ]);
        const { items: devs, total: t, totalPages: tp } = devRes.data;
        setItems(devs);
        setTotal(t);
        setTotalPages(Math.max(1, tp));
        const org = orgRes.data.find((o) => o.organisationId === orgId);
        if (org) setOrgName(org.name);
      } catch (err) {
        setError("Failed to load devices.");
        console.error(err);
      } finally {
        setLoading(false);
      }
    },
    [orgId]
  );

  useEffect(() => {
    loadPage(currentPage);
  }, [currentPage, loadPage]);

  const handleAddOrEdit = async (deviceData) => {
    try {
      const payload = {
        organisationId: orgId,
        name: deviceData.name,
        type: deviceData.type,
        description: deviceData.description,
        ratedPowerWatts: parseFloat(deviceData.ratedPowerWatts) || 0,
      };

      if (editingDevice) {
        await api.put(`/Device/${editingDevice.deviceId}`, payload);
      } else {
        await api.post("/Device", payload);
      }

      setEditingDevice(null);
      setIsFormOpen(false);

      if (!editingDevice) {
        // New device: jump to the last page so the user sees it
        const newTotal = total + 1;
        const lastPage = Math.ceil(newTotal / PAGE_SIZE);
        if (lastPage !== currentPage) {
          goToPage(lastPage);
        } else {
          loadPage(currentPage);
        }
      } else {
        loadPage(currentPage);
      }
    } catch (err) {
      console.error("Failed to save device:", err);
      alert("Failed to save device.");
    }
  };

  const handleDelete = async (deviceId) => {
    if (!window.confirm("Delete this device?")) return;
    try {
      await api.delete(`/Device/${deviceId}`);
      const newTotal = total - 1;
      const maxPage = Math.max(1, Math.ceil(newTotal / PAGE_SIZE));
      const targetPage = Math.min(currentPage, maxPage);
      if (targetPage !== currentPage) {
        goToPage(targetPage);
      } else {
        loadPage(currentPage);
      }
    } catch (err) {
      console.error("Failed to delete device:", err);
    }
  };

  const openEdit = (device) => {
    setEditingDevice(device);
    setIsFormOpen(true);
  };

  if (loading) {
    return <Loading label="Loading devices..." />;
  }

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center h-64 gap-4">
        <p style={{ color: "var(--red)", fontSize: "0.875rem" }}>{error}</p>
        <button
          onClick={() => navigate("/organisations")}
          style={{ color: "var(--blue)", fontSize: "0.875rem", background: "none", border: "none", cursor: "pointer" }}
        >
          ← Back to Organisations
        </button>
      </div>
    );
  }

  return (
    <div className="fade-in p-5">
      {/* Header */}
      <div className="flex justify-between items-center mb-4">
        <div className="flex items-center gap-3">
          <button
            onClick={() => navigate("/organisations")}
            style={{
              fontSize: "0.8125rem", color: "var(--text-muted)", background: "none",
              border: "none", cursor: "pointer", padding: 0,
            }}
          >
            ← Back
          </button>
          <span style={{ fontSize: "0.8125rem", color: "var(--text-secondary)" }}>
            Organisation:{" "}
            <span style={{ color: "var(--text-primary)", fontWeight: 500 }}>{orgName}</span>
          </span>
        </div>
        <button
          onClick={() => { setEditingDevice(null); setIsFormOpen(true); }}
          className="btn-primary"
          style={{ fontSize: "0.8125rem", padding: "0.375rem 0.875rem" }}
        >
          + Add Device
        </button>
      </div>

      <DeviceForm
        isOpen={isFormOpen}
        onClose={() => { setEditingDevice(null); setIsFormOpen(false); }}
        onSubmit={handleAddOrEdit}
        initialData={editingDevice}
      />

      {total === 0 && !loading ? (
        <div
          className="flex items-center justify-center"
          style={{
            height: 200,
            background: "var(--bg-card)",
            border: "1px solid var(--border)",
            borderRadius: "var(--radius-lg)",
            color: "var(--text-muted)",
            fontSize: "0.875rem",
          }}
        >
          No devices for this organisation yet.
        </div>
      ) : (
        <div
          style={{
            background: "var(--bg-card)",
            border: "1px solid var(--border)",
            borderRadius: "var(--radius-lg)",
            overflow: "hidden",
          }}
        >
          <table className="w-full" style={{ borderCollapse: "collapse" }}>
            <thead>
              <tr style={{ borderBottom: "1px solid var(--border)" }}>
                {["Name", "Type", "Description", "Rated Power", "Actions"].map((h) => (
                  <th
                    key={h}
                    style={{
                      padding: "0.75rem 1rem",
                      textAlign: "left",
                      fontSize: "0.6875rem",
                      fontWeight: 600,
                      color: "var(--text-muted)",
                      textTransform: "uppercase",
                      letterSpacing: "0.06em",
                      background: "var(--bg-elevated)",
                    }}
                  >
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {items.map((device, idx) => (
                <tr
                  key={device.deviceId}
                  style={{
                    borderBottom: idx < items.length - 1 ? "1px solid var(--border)" : "none",
                    transition: "background 0.1s",
                  }}
                  onMouseEnter={(e) => (e.currentTarget.style.background = "var(--bg-elevated)")}
                  onMouseLeave={(e) => (e.currentTarget.style.background = "transparent")}
                >
                  <td style={{ padding: "0.75rem 1rem", fontSize: "0.875rem", fontWeight: 500 }}>
                    {device.name}
                  </td>
                  <td style={{ padding: "0.75rem 1rem", fontSize: "0.8125rem", color: "var(--text-secondary)" }}>
                    {device.type}
                  </td>
                  <td style={{ padding: "0.75rem 1rem", fontSize: "0.8125rem", color: "var(--text-secondary)" }}>
                    {device.description || "—"}
                  </td>
                  <td style={{ padding: "0.75rem 1rem", fontSize: "0.8125rem", color: "var(--amber)", fontWeight: 500 }}>
                    {device.ratedPowerWatts != null ? `${device.ratedPowerWatts} W` : "—"}
                  </td>
                  <td style={{ padding: "0.75rem 1rem" }}>
                    <div className="flex gap-4">
                      <button
                        onClick={() => openEdit(device)}
                        style={{ fontSize: "0.8125rem", color: "var(--blue)", background: "none", border: "none", cursor: "pointer", padding: 0 }}
                      >
                        Edit
                      </button>
                      <button
                        onClick={() => handleDelete(device.deviceId)}
                        style={{ fontSize: "0.8125rem", color: "var(--red)", background: "none", border: "none", cursor: "pointer", padding: 0 }}
                      >
                        Delete
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* pagination */}
      <div
        className="flex items-center justify-between"
        style={{ marginTop: "1rem" }}
      >
        <span style={{ fontSize: "0.75rem", color: "var(--text-muted)" }}>
          {total === 0
            ? "No devices"
            : `${(currentPage - 1) * PAGE_SIZE + 1}–${Math.min(currentPage * PAGE_SIZE, total)} of ${total} devices`}
        </span>
        <div className="flex gap-1">
          <button
            onClick={() => goToPage(currentPage - 1)}
            disabled={currentPage === 1}
            style={{
              padding: "0.25rem 0.625rem", fontSize: "0.75rem",
              background: "var(--bg-card)", border: "1px solid var(--border)",
              borderRadius: "var(--radius-sm)",
              color: currentPage === 1 ? "var(--text-muted)" : "var(--text-primary)",
              cursor: currentPage === 1 ? "not-allowed" : "pointer",
            }}
          >
            ← Prev
          </button>
          {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
            <button
              key={p}
              onClick={() => goToPage(p)}
              style={{
                padding: "0.25rem 0.5rem", fontSize: "0.75rem", minWidth: 28,
                background: p === currentPage ? "var(--blue)" : "var(--bg-card)",
                border: `1px solid ${p === currentPage ? "var(--blue)" : "var(--border)"}`,
                borderRadius: "var(--radius-sm)",
                color: p === currentPage ? "#fff" : "var(--text-muted)",
                cursor: "pointer",
              }}
            >
              {p}
            </button>
          ))}
          <button
            onClick={() => goToPage(currentPage + 1)}
            disabled={currentPage === totalPages}
            style={{
              padding: "0.25rem 0.625rem", fontSize: "0.75rem",
              background: "var(--bg-card)", border: "1px solid var(--border)",
              borderRadius: "var(--radius-sm)",
              color: currentPage === totalPages ? "var(--text-muted)" : "var(--text-primary)",
              cursor: currentPage === totalPages ? "not-allowed" : "pointer",
            }}
          >
            Next →
          </button>
        </div>
      </div>
    </div>
  );
}

