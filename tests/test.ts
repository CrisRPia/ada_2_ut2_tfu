import * as backend from "./codegen/backend.api.ts";
import { assert, deepLog, delay } from "./helpers.ts";

type Milliseconds = number;

async function test_rate_limiting() {
  console.log("Clearing up rate limiting window...")
  await delay(1000 * 12);
  console.log("Testing rate limiting.");

  async function measure_response_time(): Promise<Milliseconds> {
    const start = performance.now();
    const status = (await backend.getPing()).status;
    assert(status == 200, { status });
    const elapsed = performance.now() - start;
    return elapsed;
  }

  console.log("Testing delayed execution.");
  const maxPer12Secs = 4;
  const outputs: number[] = [];
  for (let i = 0; i < maxPer12Secs; i++) {
    outputs.push(await measure_response_time());
  }

  const last = await measure_response_time();
  assert(outputs.reduce((a, b) => a + b) * 10 < last, { outputs, last });

  console.log("Testing request denial.");

  const n = 1000;
  const results = await Promise.all(
    Array(n)
      .fill(0)
      .map(async (_) => {
        return (await backend.getPing()).status as 200 | 429;
      }),
  );

  const successCount = results.filter((s) => s == 200).length;
  const failureCount = results.filter((s) => s == 429).length;
  assert(successCount < n, { successCount });
  assert(failureCount > 0, { failureCount });
  deepLog({ successCount, failureCount });

  console.log("Rate limiting tests passed.");
}

async function test_normal_flow() {
  console.log("Testing flow.");

  const password = "my very secure password";

  const credentials = await backend.postAuthRegister({
    email: "test@gmail.com",
    masterPassword: password,
  });

  let headers = {
    headers: {
      Authorization: `Bearer ${credentials.data.token}`,
    },
  } as const;

  const unauthorizedGetStatus = (
    await backend.postClientEncryptAndUpdateVault({
      masterPassword: password,
      vaultData: {},
    })
  ).status as number;

  assert(unauthorizedGetStatus == 401, { unauthorizedGetStatus });

  const vault = {
    "www.domain.com": { username: "pepe", password: "1234" },
  } satisfies backend.VaultUpdateRequestVaultData;

  const authorizedPost = await backend.postClientEncryptAndUpdateVault(
    {
      masterPassword: password,
      vaultData: vault,
    },
    headers,
  );

  assert(authorizedPost.status == 204, { authorizedPost });

  const authorizedGetWithBadPassword = await backend.postClientDecryptVault(
    { masterPassword: "bad password" },
    headers,
  );
  assert(authorizedGetWithBadPassword.status == 400, {
    authorizedGetWithBadPassword,
  });

  const authorizedGetWithGoodPassword = await backend.postClientDecryptVault(
    { masterPassword: password },
    headers,
  );
  assert(authorizedGetWithGoodPassword.status == 200, {
    authorizedGetWithGoodPassword,
  });
  assert(
    JSON.stringify(authorizedGetWithGoodPassword.data.vaultData) ==
      JSON.stringify(vault),
    {
      authorizedGetWithGoodPassword,
    },
  );

  console.log("Flow tests passed.");
}

async function main() {
  await test_normal_flow();
  await test_rate_limiting();
}

main();
