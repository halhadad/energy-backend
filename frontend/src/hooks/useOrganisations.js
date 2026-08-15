import { useEffect } from "react";
import { useDispatch, useSelector } from "react-redux";
import { setOrganisations } from "../redux/user/userSlice";
import api from "../api/axios";

export function useOrganisations() {
    const dispatch = useDispatch();
    const isAuthenticated = useSelector((s) => s.user.isAuthenticated);

    useEffect(() => {
        if (!isAuthenticated) return;
        // Always refresh on mount when authenticated, even if we have cached orgs,
        // so that new orgs created in the session appear in the selector.
        api.get("/Organisation")
            .then((res) => { dispatch(setOrganisations(res.data)); })
            .catch(() => { });
    }, [isAuthenticated, dispatch]);
}
