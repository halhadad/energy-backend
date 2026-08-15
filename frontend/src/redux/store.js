import { configureStore, combineReducers } from '@reduxjs/toolkit';
import userReducer from './user/userSlice';
import { persistReducer, persistStore, createMigrate } from 'redux-persist';
import storage from 'redux-persist/lib/storage';

const migrations = {
    // v2 adds organisations and selectedOrgId that didn't exist before
    2: (state) => ({
        ...state,
        user: {
            ...state.user,
            organisations: state.user?.organisations ?? [],
            selectedOrgId: state.user?.selectedOrgId ?? null,
        },
    }),
    // v3 renames organizations -> organisations
    3: (state) => ({
        ...state,
        user: {
            ...state.user,
            organisations: state.user?.organisations ?? state.user?.organizations ?? [],
        },
    }),
};

const rootReducer = combineReducers({
    user: userReducer,
});

const persistConfig = {
    key: 'root',
    version: 3,           // bumping this runs the migration on first load
    storage,
    migrate: createMigrate(migrations, { debug: false }),
};

const persistedReducer = persistReducer(persistConfig, rootReducer);

export const store = configureStore({
    reducer: persistedReducer,
    middleware: (getDefaultMiddleware) =>
        getDefaultMiddleware({
            serializableCheck: false,
        }),
});

export const persistor = persistStore(store);