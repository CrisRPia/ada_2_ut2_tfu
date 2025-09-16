export function deepLog<T>(obj: T): T {
    console.dir(obj, { depth: null });

    return obj;
}

export function delay(ms: number) {
    return new Promise( resolve => setTimeout(resolve, ms) );
}

export function assert<T>(clause: boolean, context: T): T {
    if (!clause) {
        console.error("Assertion error");
        deepLog(context)
        throw -1;
    }
    console.info("Assertion success");
    return context;
}
