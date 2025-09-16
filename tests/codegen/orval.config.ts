import { ConfigExternal } from "orval";

export default {
    backend: {
        input: {
            target: "./backend.schema.json",
        },
        output: {
            baseUrl: "http://localhost:8080/",
            target: "backend.api.ts",
            prettier: true,
            client: "fetch",
        },
    },
} satisfies ConfigExternal;
