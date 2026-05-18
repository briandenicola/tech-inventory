# F019: Household Inventory AI Chatbot

**Status**: backlog
**Priority**: P4
**Effort**: L
**Value**: medium
**Risk**: medium
**Target release**: TBD (post-v1, after F018 establishes the Ollama pattern)
**Created**: 2026-05-17
**Owner**: unassigned

## Problem
The inventory accumulates a large amount of information that family members
need to query in natural ways, often when they don't know the right filter
or column to look at. Common questions that are awkward to answer via lists
and filters today:

- "Whose laptop is the oldest?"
- "What warranties expire this year?"
- "How much have we spent on Apple stuff total?"
- "Where's the spare HDMI cable?"
- "Which devices haven't been updated in over a year?"
- "What's connected to the IoT VLAN?"
- "Who has the most devices assigned to them?"

Building UI filters and reports for every possible question is intractable.
A natural-language chat interface backed by structured queries against the
inventory data is a much higher-leverage interaction for a household.

This must be done **without sending family data to any third-party AI service**,
in keeping with the project's self-hosted, privacy-first stance.

## Proposed Solution
A chat interface inside the PWA that lets any authenticated user ask questions
about the household's devices in natural language. The chatbot runs entirely
locally:

1. User question goes to a local **Ollama** instance with a text model
   (e.g., `llama3.1:8b`, `qwen2.5:7b`, `mistral:7b`)
2. The model is given the database schema, a list of available query tools,
   and instructions to either (a) call a tool to fetch structured data, or
   (b) answer from general knowledge with a clear "I don't have data on that"
   if outside scope
3. Tool calls go to our existing **REST API** (the chatbot is just another
   API client), respecting the calling user's role and permissions
4. Results are summarized by the model and presented as natural language plus,
   when relevant, a structured snippet (table, count, list with links)

**Critical principles**:
- The chatbot **uses the existing API**, not direct DB access. This means it
  automatically inherits authz (a Viewer asking about devices only sees what
  a Viewer is allowed to see)
- All inference is local; no external egress
- Responses are **deterministic about facts**, not creative — the model
  paraphrases tool output but does not invent device records
- Users can always see what tools were called and with what parameters
  (transparency / debuggability)

## User Stories
- **U-AI9**: As a Member, I ask "what's the oldest laptop?" and get a single
  answer naming the device, with a link to its detail page.
- **U-AI10**: As an Admin, I ask "how much did we spend on Apple devices last
  year?" and get a total with a breakdown by device.
- **U-AI11**: As a Viewer, I ask "where's the soldering iron?" and get its
  location — or "you don't have one in inventory" if it's not recorded.
- **U-AI12**: As a Member, I ask a question that the AI can't answer with
  available tools (e.g., "should I upgrade my router?") and get a clear "I
  can only answer questions about your inventory" response rather than
  hallucinated advice.
- **U-AI13**: As any user, I can see the underlying data the AI used (e.g.,
  "I called `list_devices(category=laptop, sort=age_desc, limit=1)`") so I
  can trust the answer.
- **U-AI14**: As an Admin, I disable the chatbot entirely via feature flag,
  so households that don't run Ollama have a clean UI.
- **U-AI15**: As any user, I see clear loading states while the AI thinks,
  and a friendly error message if Ollama is unreachable.
- **U-AI16**: As a Viewer, when I ask about devices, the chatbot only returns
  information that my role permits — it cannot be used to bypass authz.
- **U-AI17**: As an Admin, I can review chatbot interaction logs (questions
  asked, tools called, responses) for the household so I understand how the
  feature is used and can spot anomalies.

## Acceptance Criteria
- [ ] Chat interface available as a dedicated route (e.g., `/ask`) in the PWA
- [ ] Questions are sent to a local Ollama endpoint configured via env var
- [ ] No HTTP traffic from the AI service container reaches any external host
      (verified by network policy and audit)
- [ ] Tool calling implemented: the model can request structured data via a
      defined set of tools (see Open Questions for tool catalog)
- [ ] **Every tool call goes through the existing REST API** with the calling
      user's auth token; no direct DB access from the AI layer
- [ ] **Authz is preserved**: a Viewer asking the chatbot a question gets the
      same data they'd see in the UI, no more
- [ ] When the model cannot answer from available tools, it responds with a
      clear scope-limiting message; it does **not** invent device records,
      serial numbers, or facts
- [ ] Tool calls and results are visible to the user (expandable "show
      reasoning" affordance) for transparency
- [ ] Conversation history is scoped to the current session by default;
      persistent history is opt-in per user
- [ ] If Ollama is unreachable, chat shows a clear error state and disables
      input; existing conversation history remains visible
- [ ] Streaming responses (tokens appear as generated) for perceived latency
- [ ] Feature flag (`features.aiChatbot`) disables the entire feature at API
      and UI levels
- [ ] Audit log records: who asked, when, the question text, tools called,
      tool parameters, response summary (no raw model output to keep logs
      manageable)
- [ ] AI inference latency target: first token < 3s, complete response < 30s
      for typical questions on reference hardware
- [ ] OpenAPI documents the new endpoints: `POST /api/v1/chat/messages`
      (with SSE / chunked transfer for streaming)
- [ ] Rate limiting per user (e.g., 30 questions/hour) to protect shared
      Ollama capacity
- [ ] WCAG 2.2 AA compliant per constitution §6.5.6:
  - Live region for streaming responses (`aria-live="polite"`)
  - Keyboard-only operation (send, copy, expand reasoning)
  - Focus management on response complete
- [ ] No PII in prompts to Ollama beyond what's necessary (e.g., owner *names*
      yes; email addresses no, unless the user explicitly asked about that)
- [ ] Test coverage ≥ 85% on the chat orchestration / tool-routing layer
- [ ] Integration tests use a mocked Ollama client (no model needed in CI)
- [ ] At least one E2E happy path test using locally-hosted Ollama
      (gated to local dev / nightly CI)

## Out of Scope
- Voice input / speech-to-text (separate feature)
- Multi-turn agentic workflows ("plan my device upgrades for next year")
- Write operations via chat in v1 ("delete that old laptop" — too risky
  with LLM ambiguity; require explicit UI confirmation flow if revisited)
- Cross-household or external knowledge (model knowledge is paraphrasing
  only, never authoritative)
- Generating reports, charts, or images
- Push notifications or proactive AI suggestions ("you haven't updated X
  in 6 months") — separate feature
- RAG / embedding-based document search across attachments and notes —
  candidate for a follow-up feature once the tool-calling pattern is proven
- Model fine-tuning on household data
- Sharing chat history across users
- Selecting a model per question; admin picks one globally
- Translation / multilingual support beyond what the base model provides

## Dependencies
- **F001 Core API** — chatbot is a consumer of the REST API
- **F002 Authentication** — chatbot inherits user context for authz
- **F018 AI Photo-to-Device** — should ship first to establish:
  - The Ollama integration pattern
  - The AI service container architecture
  - Network egress restriction patterns
  - Feature flag conventions for AI features
- **Audit log** infrastructure for AI interaction logging
- New ADR: `docs/adr/00XX-ai-tool-calling-via-rest-api.md` documenting the
  architectural choice to route tool calls through the API rather than
  direct DB access
- New ADR: `docs/adr/00XX-chatbot-authz-model.md` documenting how the
  chatbot preserves role-based access

## Open Questions
- **Q1**: Which Ollama model is the default? Tool-calling support varies
  significantly. Candidates: `llama3.1:8b` (tools support), `qwen2.5:7b`
  (strong tool use), `mistral-nemo`. Needs evaluation on reference hardware.
- **Q2**: What is the **tool catalog**? Initial proposal:
  - `list_devices(filters, sort, limit)`
  - `get_device(id)`
  - `count_devices(filters)`
  - `summarize_spending(filters, group_by)`
  - `find_warranties_expiring(within_days)`
  - `list_owners()`
  - `list_locations()`
  - `list_categories()`
  Needs broader review — too many tools = confused model; too few = limited.
- **Q3**: How do we constrain the model from hallucinating tool calls or
  inventing arguments? JSON-schema enforcement? Re-prompt on parse failure?
- **Q4**: Do we share the AI service container with F018 (one service,
  multiple capabilities) or run separate containers? Probably shared, but
  the API contract between AI service and main API needs careful design.
- **Q5**: How is conversation context maintained without ballooning prompts?
  Sliding window? Summary-on-truncate?
- **Q6**: How do we handle ambiguous questions ("who has the most stuff?")
  — does the model ask clarifying questions, or pick a reasonable default
  and disclose the assumption?
- **Q7**: Tool calls run as the user — but does the AI service container
  hold the user's token, or do we use a delegated service token with a
  user-scoping header that the API enforces? (Security review needed.)
- **Q8**: What's the prompt template? It must include:
  - System instructions (be factual, use tools, refuse out-of-scope)
  - Tool catalog with JSON schemas
  - Recent conversation history
  - Versioned so we can iterate without breaking audit records
- **Q9**: How long do we keep chat history per user? 30 days? Per-user
  retention setting?
- **Q10**: What happens when a tool call returns 0 results? Model should
  say "no matches" — but we need to make sure it doesn't fall back to
  hallucinated guesses to "be helpful."
- **Q11**: Streaming protocol — SSE or WebSocket? SSE is simpler and works
  with our HTTP-only stack; preferred unless we have specific bidirectional
  needs.
- **Q12**: Cost of getting this wrong — a confidently-wrong AI answer about
  "device X is in the basement" when it's not could be more annoying than
  no answer. How do we measure and tune for correctness?

## Notes / Research
- Ollama supports tool calling in recent versions (0.3+) for models that
  emit structured tool-call output (Llama 3.1+, Qwen 2.5, etc.)
- The tool-call pattern is essentially: model emits a JSON describing the
  desired call → orchestrator executes it → result is fed back to model →
  model produces final natural-language answer
- This pattern keeps the model's role narrow: route + paraphrase. It does
  not need to know the schema deeply; it just needs to pick the right tool
- Tradeoffs:
  - Tool calling reduces hallucination dramatically vs. raw LLM Q&A
  - It adds latency (potentially multiple round-trips)
  - Some smaller models struggle with structured output; pick carefully
- Alternative considered & rejected: **text-to-SQL**. Even with local models,
  text-to-SQL is brittle, opaque to audit, easy to misuse, and hard to
  align with role-based