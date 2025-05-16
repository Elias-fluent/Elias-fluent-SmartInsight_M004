import { useEffect, useState } from "react";
import { useAnnounce } from "../../hooks/useAnnounce";

import type { ToastActionElement, ToastProps } from "./toast";

const TOAST_LIMIT = 3;
const TOAST_REMOVE_DELAY = 5000;

type ToasterToast = ToastProps & {
  id: string;
  title?: React.ReactNode;
  description?: React.ReactNode;
  action?: ToastActionElement;
};

const actionTypes = {
  ADD_TOAST: "ADD_TOAST",
  UPDATE_TOAST: "UPDATE_TOAST",
  DISMISS_TOAST: "DISMISS_TOAST",
  REMOVE_TOAST: "REMOVE_TOAST",
} as const;

let count = 0;

const genId = () => {
  count = (count + 1) % Number.MAX_VALUE;
  return count.toString();
};

type ActionType = typeof actionTypes;

type Action =
  | {
      type: ActionType["ADD_TOAST"];
      toast: ToasterToast;
    }
  | {
      type: ActionType["UPDATE_TOAST"];
      toast: Partial<ToasterToast>;
    }
  | {
      type: ActionType["DISMISS_TOAST"];
      toastId?: string;
    }
  | {
      type: ActionType["REMOVE_TOAST"];
      toastId?: string;
    };

interface State {
  toasts: ToasterToast[];
}

const toastTimeouts = new Map<string, ReturnType<typeof setTimeout>>();

const reducer = (state: State, action: Action): State => {
  switch (action.type) {
    case actionTypes.ADD_TOAST:
      return {
        ...state,
        toasts: [action.toast, ...state.toasts].slice(0, TOAST_LIMIT),
      };

    case actionTypes.UPDATE_TOAST:
      return {
        ...state,
        toasts: state.toasts.map((t) =>
          t.id === action.toast.id ? { ...t, ...action.toast } : t
        ),
      };

    case actionTypes.DISMISS_TOAST:
      const { toastId } = action;

      // Dismiss all toasts
      if (toastId === undefined) {
        return {
          ...state,
          toasts: state.toasts.map((t) => ({
            ...t,
            open: false,
          })),
        };
      }

      // Dismiss specific toast
      return {
        ...state,
        toasts: state.toasts.map((t) =>
          t.id === toastId ? { ...t, open: false } : t
        ),
      };

    case actionTypes.REMOVE_TOAST:
      if (action.toastId === undefined) {
        return {
          ...state,
          toasts: [],
        };
      }
      return {
        ...state,
        toasts: state.toasts.filter((t) => t.id !== action.toastId),
      };
    default:
      return state;
  }
};

const useToast = () => {
  const [state, setState] = useState<State>({ toasts: [] });
  const { announcePolite } = useAnnounce();

  useEffect(() => {
    state.toasts.forEach((toast) => {
      if (toast.open) {
        if (toast.title) {
          announcePolite(`${toast.title}: ${toast.description || ''}`);
        } else if (toast.description) {
          announcePolite(toast.description.toString());
        }
      }
    });
  }, [state.toasts, announcePolite]);

  const dispatch = (action: Action) => {
    setState((prevState) => reducer(prevState, action));
  };

  const toast = (props: Omit<ToasterToast, "id">) => {
    const id = genId();
    const newToast = { id, open: true, ...props };

    dispatch({ type: actionTypes.ADD_TOAST, toast: newToast });

    if (toastTimeouts.has(id)) {
      clearTimeout(toastTimeouts.get(id));
    }

    toastTimeouts.set(
      id,
      setTimeout(() => {
        dispatch({ type: actionTypes.DISMISS_TOAST, toastId: id });
        setTimeout(() => {
          dispatch({ type: actionTypes.REMOVE_TOAST, toastId: id });
        }, 300);
      }, TOAST_REMOVE_DELAY)
    );

    return id;
  };

  const update = (id: string, updateProps: Partial<ToasterToast>) => {
    if (updateProps.id) delete updateProps.id;
    
    dispatch({
      type: actionTypes.UPDATE_TOAST,
      toast: { id, ...updateProps },
    });
  };

  const dismiss = (id?: string) => {
    dispatch({ type: actionTypes.DISMISS_TOAST, toastId: id });

    if (id) {
      setTimeout(() => {
        dispatch({ type: actionTypes.REMOVE_TOAST, toastId: id });
      }, 300);
    }
  };

  return {
    toast,
    update,
    dismiss,
    toasts: state.toasts,
  };
};

export { useToast }; 