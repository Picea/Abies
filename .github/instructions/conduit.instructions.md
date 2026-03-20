---
description: 'Guidelines for the Realworld Conduit application'
---

# Conduit

The Conduit application is a real-world example of a web application built to showcase the abilities of the Abies framework. Picea.Abies.Conduit is that application in this solution.

## Specification

All specifications for the Conduit application can be found at the website: https://docs.realworld.show/ . The implementation of the showcase app MUST follow these specifications.

## Testing

All user journeys MUST have an E2E test and integration tests. The user journeys are described in the specifications.

## Editing abies.js

Always edit `Picea.Abies.Browser/wwwroot/abies.js` for changes related to the Abies framework. It is copied to consuming projects at build time.