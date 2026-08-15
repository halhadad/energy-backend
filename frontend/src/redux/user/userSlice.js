import { createSlice } from "@reduxjs/toolkit";
const initialState = {
  current_user: null,
  accessToken: null,
  refreshToken: null,
  isAuthenticated: false,
  loading: false,
  error: null,
  unseenAlertCount: 0,
  activeAlertCount: 0,
  organisations: [],
  selectedOrgId: null,
};
const userSlice = createSlice({
  name: "user",
  initialState,
  reducers: {
    loginRequest: (state) => { state.loading = true; },
    loginSuccess: (state, action) => {
      state.current_user = action.payload.user;
      state.accessToken = action.payload.accessToken;
      state.refreshToken = action.payload.refreshToken;
      state.isAuthenticated = true;
      state.loading = false;
      state.error = null;
      localStorage.setItem("accessToken", action.payload.accessToken);
      localStorage.setItem("refreshToken", action.payload.refreshToken);
      localStorage.setItem("user", JSON.stringify(action.payload.user));
    },
    loginFailure: (state, action) => {
      state.error = action.payload;
      state.loading = false;
    },
    logout: (state) => {
      state.error = null;
      state.current_user = null;
      state.accessToken = null;
      state.refreshToken = null;
      state.isAuthenticated = false;
      state.unseenAlertCount = 0;
      state.activeAlertCount = 0;
      state.organisations = [];
      state.selectedOrgId = null;
      localStorage.clear();
    },
    incrementUnseenAlerts: (state) => {
      state.unseenAlertCount += 1;
    },
    clearUnseenAlerts: (state) => {
      state.unseenAlertCount = 0;
    },
    // number of unresolved alerts, drives the red dot on the Alerts nav item
    setActiveAlertCount: (state, action) => {
      state.activeAlertCount = Math.max(0, action.payload);
    },
    incrementActiveAlerts: (state) => {
      state.activeAlertCount += 1;
    },
    decrementActiveAlerts: (state) => {
      state.activeAlertCount = Math.max(0, state.activeAlertCount - 1);
    },
    setOrganisations: (state, action) => {
      state.organisations = action.payload;
      // Only set selectedOrgId if not already set or current selection is stale
      const ids = action.payload.map((o) => o.organisationId);
      if (!state.selectedOrgId || !ids.includes(state.selectedOrgId)) {
        state.selectedOrgId = action.payload.length > 0 ? action.payload[0].organisationId : null;
      }
    },
    setSelectedOrg: (state, action) => {
      state.selectedOrgId = action.payload;
    },
  },
});
export const {
  loginRequest,
  loginSuccess,
  loginFailure,
  logout,
  incrementUnseenAlerts,
  clearUnseenAlerts,
  setActiveAlertCount,
  incrementActiveAlerts,
  decrementActiveAlerts,
  setOrganisations,
  setSelectedOrg,
} = userSlice.actions;
export default userSlice.reducer;